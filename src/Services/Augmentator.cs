using Darknet.Dataset.Merger.Model;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Darknet.Dataset.Merger.Services
{
    public class LocalAnnotationContext
    {
        public ImageInfo Image { get; set; }
        public string SourceAnnotationText { get; set; }
    }

    public class AImage
        : IDisposable
    {
        private IMagickImage<ushort> _current;
        private IMagickImage<ushort> _source;

        public int Width => _current.Width;
        public int Height => _current.Height;

        public AImage(string filepath)
        {
            _current = new MagickImage(filepath);
            _source = _current.Clone();
        }

        public AImage(IMagickImage<ushort> image)
        {
            _current = image;
            _source = _current.Clone();
        }

        public void Resize(int width, int height)
        {
            _current.InterpolativeResize(width, height, PixelInterpolateMethod.Mesh);
        }

        public void Reset()
        {
            _current.Dispose();
            _current = _source.Clone();
        }

        public void SaveAsSource()
        {
            _source.Dispose();
            _source = _current.Clone();
        }

        public void Write(string filepath)
        {
            _current.Write(filepath);
        }

        public AImage Crop(int x, int y, int width, int height)
        {
            var rect = new MagickGeometry(x, y, width, height);
            rect.IgnoreAspectRatio = true;
            var ai = _current.Clone();
            ai.Crop(rect);
            return new AImage(ai);
        }

        public AImage Clone()
        {
            var ai = _current.Clone();
            return new AImage(ai);
        }

        private const float CROPED_BBOX_AREA_MIN_PART = 0.1f;
        public IEnumerable<Tuple<AImage, List<Annotation>>> Crop(IEnumerable<Annotation> annotations, int cut_width, int cut_height, bool overlap)
        {
            var xs = overlap ? (int)(cut_width * .8f) : cut_width;
            var ys = overlap ? (int)(cut_height * .8f) : cut_height;
            for (var x = 0; x < this._current.Width - xs; x += xs)
            {
                var startx = x;
                var dx = (x + cut_width) - this._current.Width;
                if (dx > 0)
                {
                    startx -= dx;
                }
                for (var y = 0; y < this._current.Height - ys; y += ys)
                {
                    var starty = y;
                    var dy = (y + cut_height) - this._current.Height;
                    if (dy > 0)
                    {
                        starty -= dy;
                    }
                    var cropped_annotations = new List<Annotation>();
                    foreach (var a in annotations)
                    {
                        var bbox_cx = a.Cx * this._current.Width - startx;
                        var bbox_cy = a.Cy * this._current.Height - starty;
                        var bbox_w = a.Width * this._current.Width;
                        var bbox_h = a.Height * this._current.Height;
                        var bbox_x = bbox_cx - bbox_w / 2.0f;
                        var bbox_y = bbox_cy - bbox_h / 2.0f;
                        var left = (float)Math.Max(bbox_x, 0);
                        var right = (float)Math.Min(bbox_x + bbox_w, cut_width);
                        var top = (float)Math.Max(bbox_y, 0);
                        var bottom = (float)Math.Min(bbox_y + bbox_h, cut_height);
                        var width = (float)(right - left);
                        var height = (float)(bottom - top);

                        if (width > 0 && height > 0)
                        {
                            var bbox_area = bbox_w * bbox_h;
                            var over_area = width * height;
                            if ((over_area / bbox_area) > CROPED_BBOX_AREA_MIN_PART)
                            {
                                var acx = left + width / 2.0f;
                                var acy = top + height / 2.0f;
                                var cropped_annotation = new Annotation
                                {
                                    Class = a.Class,
                                    Cx = acx / cut_width,
                                    Cy = acy / cut_height,
                                    Width = width / cut_width,
                                    Height = height / cut_height,
                                    Label = a.Label
                                };
                                cropped_annotations.Add(cropped_annotation);
                            }
                        }
                    }
                    yield return Tuple.Create(this.Crop(startx, starty, cut_width, cut_height), cropped_annotations);
                }
            }
        }

        public void Dispose()
        {
            _source.Dispose();
            _current.Dispose();
        }

        #region Augmentations
        public void Blur()
        {
            _current.AdaptiveBlur();
        }
        public void Brightness()
        {
            _current.BrightnessContrast(new Percentage(50), new Percentage(10));
        }
        public void Contrast()
        {
            _current.Contrast(true);
        }
        public void Flip()
        {
            _current.Flip();
        }
        public void Flop()
        {
            _current.Flop();
        }
        public void Grayscale()
        {
            _current.Grayscale();
        }

        public void Moonlight()
        {
            _current.BlueShift();
        }

        public void Noise()
        {
            _current.AddNoise(NoiseType.Laplacian, 2.0);
        }

        public void Charcoal()
        {
            _current.Charcoal();
        }

        public void SepiaTone()
        {
            _current.SepiaTone();
        }
        #endregion
    }

    public static class Augmentator
    {
        private const float MAX_SIDE_LIMIT = 1216.0f;

        public static int Augmentate(ImageInfo imageInfo, AugmentationContext context, AugmentationOptions options)
        {
            var sb = new StringBuilder();
            foreach (var a in imageInfo.Annotations)
            {
                if (context.Classes.ContainsKey(a.Label))
                {
                    sb.Append($"{context.Classes[a.Label]} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                    sb.Append("\n");
                }
            }
            var local = new LocalAnnotationContext { SourceAnnotationText = sb.ToString(), Image = imageInfo };
            var resize = new Func<AImage, AImage>(img =>
            {
                if (img.Width > MAX_SIDE_LIMIT || img.Height > MAX_SIDE_LIMIT)
                {
                    float scale = 1.0f;
                    if (img.Width > img.Height)
                    {
                        scale = MAX_SIDE_LIMIT / img.Width;
                    }
                    else
                    {
                        scale = MAX_SIDE_LIMIT / img.Height;
                    }
                    img.Resize((int)(img.Width * scale), (int)(img.Height * scale));
                    img.SaveAsSource();
                }
                return img;
            });
            var store = new Action<AImage, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextAugFileNames();
                ifac.Write(imgName);
                File.WriteAllText(lblName, ann);
            });
            using (var factory = new AImage(imageInfo.FilePath))
            {
                using (var original = resize(factory.Clone()))
                {
                    // Save resized original image
                    var (imgName, lblName) = context.GetNextObjFileNames();
                    original.Write(imgName);
                    File.WriteAllText(lblName, local.SourceAnnotationText);

                    ApplyAugmentations(original, local, context, options);
                }
                if (local.Image.Annotations.Any()
                    && options.Cut
                    && options.CutWidth > 0
                    && options.CutHeight > 0
                    && options.CutWidth < (factory.Width / 1.5)
                    && options.CutHeight < (factory.Height / 1.5))
                {
                    foreach (var cropped in factory.Crop(local.Image.Annotations, options.CutWidth, options.CutHeight, options.CutOverlaps))
                    {
                        using (cropped.Item1)
                        {
                            var sbCropped = new StringBuilder();
                            foreach (var cropped_annotation in cropped.Item2)
                            {
                                sbCropped.Append($"{context.Classes[cropped_annotation.Label]} {cropped_annotation.Cx.ConvertToString()} {cropped_annotation.Cy.ConvertToString()} {cropped_annotation.Width.ConvertToString()} {cropped_annotation.Height.ConvertToString()}");
                                sbCropped.Append("\n");
                            }
                            if (sbCropped.Length > 0) // no augmentation for empty crops
                            {
                                var croppedContext = new LocalAnnotationContext { SourceAnnotationText = sbCropped.ToString(), Image = new ImageInfo(imageInfo.FilePath, imageInfo.TrainType, cropped.Item2) };
                                using (var croppedAndResized = resize(cropped.Item1))
                                {
                                    store(croppedAndResized, croppedContext.SourceAnnotationText);
                                    ApplyAugmentations(cropped.Item1, croppedContext, context, options);
                                }
                            }
                        }
                    }
                }
            }
            return context.Counter;
        }

        private static void ApplyAugmentations(AImage factory, LocalAnnotationContext localContext, AugmentationContext context, AugmentationOptions options)
        {
            var store = new Action<AImage, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextAugFileNames();
                ifac.Write(imgName);
                File.WriteAllText(lblName, ann);
            });
            var app = new Action<AImage, Func<AImage, Action>>((img, gf) =>
            {
                gf(img).Invoke();
                store(img, localContext.SourceAnnotationText);
                img.Reset();
            });

            if (options.Blur) app.Invoke(factory, im => im.Blur);
            if (options.Brightness) app.Invoke(factory, im => im.Brightness);
            if (options.Contrast) app.Invoke(factory, im => im.Contrast);
            if (options.Grayscale) app.Invoke(factory, im => im.Grayscale);
            if (options.Moonlight) app.Invoke(factory, im => im.Moonlight);
            if (options.Noise) app.Invoke(factory, im => im.Noise);
            if (options.Charcoal) app.Invoke(factory, im => im.Charcoal);
            if (options.Sepia) app.Invoke(factory, im => im.SepiaTone);

            if (options.Mirrors)
            {
                var horizontallAnn = new StringBuilder();
                var verticalAnn = new StringBuilder();
                foreach (var a in localContext.Image.Annotations)
                {
                    if (context.Classes.ContainsKey(a.Label))
                    {
                        horizontallAnn.Append($"{context.Classes[a.Label]} {(1.0f - a.Cx).ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                        horizontallAnn.Append("\n");
                        verticalAnn.Append($"{context.Classes[a.Label]} {a.Cx.ConvertToString()} {(1 - a.Cy).ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                        verticalAnn.Append("\n");
                    }
                }
                factory.Flop();
                store(factory, horizontallAnn.ToString());
                factory.Reset();
                factory.Flip();
                store(factory, verticalAnn.ToString());
                factory.Reset();
            }

            if (options.Rotation)
            {

            }
            if (options.Shifts)
            {

            }
            if (options.Stretching)
            {
            }

            /*
             _current.CopyPixels()
             */
            if (options.BBoxMirrors)
            {
                foreach (var a in localContext.Image.Annotations)
                {
                    var bbox_cx = a.Cx * factory.Width;
                    var bbox_cy = a.Cy * factory.Height;
                    var bbox_w = a.Width * factory.Width;
                    var bbox_h = a.Height * factory.Height;
                    var bbox_x = bbox_cx - bbox_w / 2.0f;
                    var bbox_y = bbox_cy - bbox_h / 2.0f;
                    var rect = new Rectangle((int)bbox_x, (int)bbox_y, (int)bbox_w, (int)bbox_h);

                    /*using (var box = crop(imageFactory, rect))
                    {
                        box.Flip(Environment.TickCount % 2 == 0);
                        imageFactory.Overlay(new ImageProcessor.Imaging.ImageLayer() { Image = box.Image, Position = new Point((int)bbox_x, (int)bbox_y), Size = new Size((int)bbox_w, (int)bbox_h), Opacity = 0 });
                    }*/
                }
                //store(imageFactory, localContext.SourceAnnotationText);
            }
            if (options.BBoxRotation)
            { 
            }
            if (options.BBoxShifts)
            { 
            }
            if (options.BBoxStretching)
            { 
            }
        }
    }
}