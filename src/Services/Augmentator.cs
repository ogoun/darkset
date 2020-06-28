using Darknet.Dataset.Merger.Model;
using ImageProcessor;
using ImageProcessor.Imaging.Filters.Photo;
using ImageProcessor.Imaging.Formats;
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

    public static class Augmentator
    {
        private const float MAX_SIDE_LIMIT = 1216.0f;
        private const float CROPED_BBOX_AREA_MIN_PART = 0.1f;

        public static int Augmentate(ImageInfo imageInfo, AugmentationContext context)
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


            byte[] photoBytes = File.ReadAllBytes(imageInfo.FilePath);
            ISupportedImageFormat format = new JpegFormat { Quality = 100 };
            var store = new Action<ImageFactory, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextFileNames();
                ifac.Format(format).Save(imgName);
                File.WriteAllText(lblName, ann);
            });
            var temporary = new Func<ImageFactory, ImageFactory>(f =>
            {
                using (var ms = new MemoryStream())
                {
                    f.Save(ms);
                    ms.Position = 0;
                    var temp_if = new ImageFactory(preserveExifData: true);
                    temp_if.Load(ms);
                    return temp_if;
                }
            });
            var crop = new Func<ImageFactory, Rectangle, ImageFactory>((f, rect) =>
            {
                var temp = temporary(f.Crop(rect));
                f.Reset();
                return temp;
            });
            var resize = new Func<ImageFactory, ImageFactory>(f =>
            {
                if (f.Image.Width > MAX_SIDE_LIMIT
                || f.Image.Height > MAX_SIDE_LIMIT)
                {
                    float scale = 1.0f;
                    if (f.Image.Width > f.Image.Height)
                    {
                        scale = MAX_SIDE_LIMIT / f.Image.Width;
                    }
                    else
                    {
                        scale = MAX_SIDE_LIMIT / f.Image.Height;
                    }
                    var temp = temporary(f.Resize(new Size { Width = (int)(f.Image.Width * scale), Height = (int)(f.Image.Height * scale) }));
                    f.Reset();
                    return temp;
                }
                return temporary(f);
            });

            using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
            {
                imageFactory.Load(photoBytes);
                using (var resized = resize(imageFactory))
                {
                    ApplyAugmentations(resized, local, context);
                }
                if (local.Image.Annotations.Any()
                    && context.Options.Cut
                    && context.Options.CutWidth > 0
                    && context.Options.CutHeight > 0
                    && context.Options.CutWidth < (imageFactory.Image.Width / 1.5)
                    && context.Options.CutHeight < (imageFactory.Image.Width / 1.5))
                {
                    var xs = context.Options.CutOverlaps ? (int)(context.Options.CutWidth * .8f) : context.Options.CutWidth;
                    var ys = context.Options.CutOverlaps ? (int)(context.Options.CutHeight * .8f) : context.Options.CutHeight;
                    var rect = new Rectangle { Width = context.Options.CutWidth, Height = context.Options.CutHeight };

                    for (var x = 0; x < imageFactory.Image.Width - xs; x += xs)
                    {
                        var startx = x;
                        var dx = (x + context.Options.CutWidth) - imageFactory.Image.Width;
                        if (dx > 0)
                        {
                            startx -= dx;
                        }
                        for (var y = 0; y < imageFactory.Image.Height - ys; y += ys)
                        {
                            var starty = y;
                            var dy = (y + context.Options.CutHeight) - imageFactory.Image.Height;
                            if (dy > 0)
                            {
                                starty -= dy;
                            }
                            rect.X = startx;
                            rect.Y = starty;

                            var sbCropped = new StringBuilder();
                            var cropped_annotations = new List<Annotation>();
                            foreach (var a in imageInfo.Annotations)
                            {
                                if (context.Classes.ContainsKey(a.Label))
                                {
                                    var bbox_cx = a.Cx * imageFactory.Image.Width - startx;
                                    var bbox_cy = a.Cy * imageFactory.Image.Height - starty;
                                    var bbox_w = a.Width * imageFactory.Image.Width;
                                    var bbox_h = a.Height * imageFactory.Image.Height;
                                    var bbox_x = bbox_cx - bbox_w / 2.0f;
                                    var bbox_y = bbox_cy - bbox_h / 2.0f;

                                    var left = (float)Math.Max(bbox_x, 0);
                                    var right = (float)Math.Min(bbox_x + bbox_w, context.Options.CutWidth);
                                    var top = (float)Math.Max(bbox_y, 0);
                                    var bottom = (float)Math.Min(bbox_y + bbox_h, context.Options.CutHeight);
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
                                                Cx = acx / rect.Width,
                                                Cy = acy / rect.Height,
                                                Width = width / rect.Width,
                                                Height = height / rect.Height,
                                                Label = a.Label
                                            };
                                            cropped_annotations.Add(cropped_annotation);
                                            sbCropped.Append($"{context.Classes[a.Label]} {cropped_annotation.Cx.ConvertToString()} {cropped_annotation.Cy.ConvertToString()} {cropped_annotation.Width.ConvertToString()} {cropped_annotation.Height.ConvertToString()}");
                                            sbCropped.Append("\n");
                                        }
                                    }
                                }
                            }
                            if (sbCropped.Length > 0) // no augmentation for empty crops
                            {
                                var croppedContext = new LocalAnnotationContext { SourceAnnotationText = sbCropped.ToString(), Image = new ImageInfo(imageInfo.FilePath, imageInfo.TrainType, cropped_annotations) };
                                using (var cropped = crop(imageFactory, rect))
                                {
                                    store(cropped, croppedContext.SourceAnnotationText);
                                    using (var croppedAndResized = resize(cropped))
                                    {
                                        ApplyAugmentations(croppedAndResized, croppedContext, context);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return context.Counter;
        }

        private static void ApplyAugmentations(ImageFactory imageFactory, LocalAnnotationContext localContext, AugmentationContext context)
        {
            var temporary = new Func<ImageFactory, ImageFactory>(f =>
            {
                using (var ms = new MemoryStream())
                {
                    f.Save(ms);
                    ms.Position = 0;
                    var temp_if = new ImageFactory(preserveExifData: true);
                    temp_if.Load(ms);
                    return temp_if;
                }
            });
            var crop = new Func<ImageFactory, Rectangle, ImageFactory>((f, rect) =>
            {
                var temp = temporary(f.Crop(rect));
                f.Reset();
                return temp;
            });
            ISupportedImageFormat format = new JpegFormat { Quality = 90 };
            var store = new Action<ImageFactory, string>((ifac, ann) =>
            {
                var (imgName, lblName) = context.GetNextFileNames();
                ifac.Format(format).Save(imgName);
                ifac.Reset();
                File.WriteAllText(lblName, ann);
            });
            if (context.Options.RGBDiffs)
            {
                imageFactory.Hue(180, true);
                store(imageFactory, localContext.SourceAnnotationText);
                imageFactory.Filter(MatrixFilters.HiSatch);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Brightness)
            {
                imageFactory.Brightness(50);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Contrast)
            {
                imageFactory.Contrast(50);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Mirrors)
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
                imageFactory.Flip(false);
                store(imageFactory, horizontallAnn.ToString());
                imageFactory.Flip(true);
                store(imageFactory, verticalAnn.ToString());
            }
            if (context.Options.Blur)
            {
                imageFactory.GaussianBlur(7);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Grayscale)
            {
                imageFactory.Filter(MatrixFilters.GreyScale);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Gotham)
            {
                imageFactory.Filter(MatrixFilters.Gotham);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Sepia)
            {
                imageFactory.Filter(MatrixFilters.Sepia);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Invert)
            {
                imageFactory.Filter(MatrixFilters.Invert);
                store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.Rotation)
            {

            }
            if (context.Options.Shifts)
            {

            }
            if (context.Options.Stretching)
            {
            }
            if (context.Options.BBoxMirrors)
            {
                //foreach (var a in localContext.Image.Annotations)
                //{
                //    var bbox_cx = a.Cx * imageFactory.Image.Width;
                //    var bbox_cy = a.Cy * imageFactory.Image.Width;
                //    var bbox_w = a.Width * imageFactory.Image.Width;
                //    var bbox_h = a.Height * imageFactory.Image.Width;
                //    var bbox_x = bbox_cx - bbox_w / 2.0f;
                //    var bbox_y = bbox_cy - bbox_h / 2.0f;
                //    var rect = new Rectangle((int)bbox_x, (int)bbox_y, (int)bbox_w, (int)bbox_h);
                //    using (var box = crop(imageFactory, rect))
                //    {
                //        box.Flip(Environment.TickCount % 2 == 0);
                //        imageFactory.Overlay(new ImageProcessor.Imaging.ImageLayer() { Image = box.Image, Position = new Point((int)bbox_x, (int)bbox_y), Size = new Size((int)bbox_w, (int)bbox_h), Opacity = 0 });
                //    }
                //}
                //store(imageFactory, localContext.SourceAnnotationText);
            }
            if (context.Options.BBoxRotation)
            {
            }
            if (context.Options.BBoxShifts)
            {
            }
            if (context.Options.BBoxStretching)
            {
            }
        }
    }
}