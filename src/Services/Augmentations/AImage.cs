using Darknet.Dataset.Merger.Model;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Darknet.Dataset.Merger.Services
{
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

        public void MirrorBoxes(BBOXES boxes)
        {
            int i = 0;
            foreach (var rect in boxes.ToMagikGeometry())
            {
                var crop_clone = _current.Clone();
                crop_clone.Crop(rect);
                if (i % 2 == 0)
                {
                    crop_clone.Flop();
                }
                else
                {
                    crop_clone.Flip();
                }
                _current.Composite(crop_clone, rect.X, rect.Y, CompositeOperator.Over);
            }
        }

        public void StretchBoxes(BBOXES original, BBOXES stretched)
        {
            var paired = original.ToMagikGeometry().Zip(stretched.ToMagikGeometry(), (o, s) => Tuple.Create(o, s));
            foreach (var pair in paired)
            {
                if (pair.Item2.X > 0 && pair.Item2.Y > 0 && pair.Item2.Width > 0 && pair.Item2.Height > 0)
                {
                    var crop_clone = _current.Clone();
                    crop_clone.Crop(pair.Item1, Gravity.Forget);
                    crop_clone.Resize(new Percentage(120), new Percentage(120));
                    _current.Composite(crop_clone, pair.Item2.X, pair.Item2.Y, CompositeOperator.Over);
                }
            }
        }

        public void RotateBoxes(BBOXES original, BBOXES rotated)
        {
            var paired = original.ToMagikGeometry().Zip(rotated.ToMagikGeometry(), (o, s) => Tuple.Create(o, s));
            foreach (var pair in paired)
            {
                if (pair.Item2.X > 0 && pair.Item2.Y > 0 && pair.Item2.Width > 0 && pair.Item2.Height > 0)
                {
                    var crop_clone = _current.Clone();
                    crop_clone.Crop(pair.Item1, Gravity.Forget);
                    crop_clone.Rotate(90);
                    _current.Composite(crop_clone, pair.Item2.X, pair.Item2.Y, CompositeOperator.Over);
                }
            }
        }

        public void BlurBoxes(BBOXES boxes)
        {
            foreach (var rect in boxes.ToMagikGeometry())
            {
                if (rect.X > 0 && rect.Y > 0 && rect.Width > 0 && rect.Height > 0)
                {
                    var crop_clone = _current.Clone();
                    crop_clone.Crop(rect);
                    crop_clone.Blur();
                    _current.Composite(crop_clone, rect.X, rect.Y, CompositeOperator.Over);
                }
            }
        }
        #endregion

        #region Helpers
        public IEnumerable<Tuple<AImage, Rectangle>> Crop(int cut_width, int cut_height, bool overlap)
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
                    yield return Tuple.Create(this.Crop(startx, starty, cut_width, cut_height), new Rectangle(startx, starty, cut_width, cut_height));
                }
            }
        }
        #endregion
    }
}
