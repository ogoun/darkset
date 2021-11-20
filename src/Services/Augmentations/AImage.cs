using Darknet.Dataset.Merger.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;

namespace Darknet.Dataset.Merger.Services
{
    public class AImage
        : IDisposable
    {
        private Image<Rgb24> _current;
        private Image<Rgb24> _source;

        public int Width => _current.Width;
        public int Height => _current.Height;

        public AImage(string filepath)
        {
            _source = Image.Load<Rgb24>(filepath);
            _current = _source.Clone();
        }

        public AImage(Image<Rgb24> image)
        {
            _source = image;
            _current = _source.Clone();
        }

        public void Resize(int width, int height)
        {
            _current.Mutate(img => img.Resize(width, height));
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

        private IImageEncoder imageEncoder = new JpegEncoder
        {
            Quality = 90,
            Subsample = JpegSubsample.Ratio444
        };
        public void Write(string filepath)
        {
            _current.Save(filepath, imageEncoder);
        }

        public AImage Crop(int x, int y, int width, int height)
        {
            var clone = _current.Clone(img => img.Crop(new Rectangle(x, y, width, height)));
            return new AImage(clone);
        }

        public AImage Clone()
        {
            return new AImage(_current.Clone());
        }

        private const float CROPED_BBOX_AREA_MIN_PART = 0.3f;
        private const float CROPED_BBOX_AREA_MAX_PART = 0.85f;
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
                    bool skip_crop_region = false;
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
                            var oa = (over_area / bbox_area);
                            if (oa <= CROPED_BBOX_AREA_MIN_PART) continue;
                            else if (oa > CROPED_BBOX_AREA_MIN_PART && oa < CROPED_BBOX_AREA_MAX_PART)
                            {
                                skip_crop_region = true;
                                break;
                            }
                            else
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
                    if (!skip_crop_region)
                    {
                        yield return Tuple.Create(this.Crop(startx, starty, cut_width, cut_height), cropped_annotations);
                    }
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
            _current.Mutate(img => img.GaussianBlur());
        }
        public void Grayscale()
        {
            _current.Mutate(img => img.Grayscale());
        }
        public void SepiaTone()
        {
            _current.Mutate(img => img.Sepia());
        }

        public void FlipHorizontal()
        {
            _current.Mutate(img => img.Flip(FlipMode.Horizontal));
        }
        public void FlipVertical()
        {
            _current.Mutate(img => img.Flip(FlipMode.Vertical));
        }
        public void FlipAndRotate(RotateMode rotate, FlipMode flip)
        {
            _current.Mutate(img => img.RotateFlip(rotate, flip));
        }
        public bool BlurBoxes(IEnumerable<Annotation> annotations)
        {
            bool has = false;
            foreach (var a in annotations)
            {
                if (a.Left >= 0 && a.Top >= 0 && a.Width > 0 && a.Height > 0)
                {
                    _current.Mutate(img => img.GaussianBlur(0.67f, a.ToMagikGeometry(Width, Height)));
                    has = true;
                }
            }
            return has;
        }
        public void LineNoize()
        {
            var rnd = new Random((int)Environment.TickCount);
            var count = Math.Max(_current.Width, _current.Height) + rnd.Next(2 * Math.Max(_current.Width, _current.Height));

            for (int i = 0; i < count; i++)
            {
                var x1 = rnd.Next(0, _current.Width);
                var y1 = rnd.Next(0, _current.Height);
                var x2 = rnd.Next(-25, 25);
                var y2 = rnd.Next(-25, 25);
                byte[] rgb = new byte[3];
                rnd.NextBytes(rgb);
                _current.Mutate(img =>
                {
                    img.DrawLines(Color.FromRgb(rgb[0], rgb[1], rgb[2]), 1, new PointF[] { new PointF(x1, y1), new PointF(x1 + x2, y1 + y2) });
                });
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
