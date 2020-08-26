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
            _source = new MagickImage(_current.ToByteArray());
        }

        public AImage(IMagickImage<ushort> image)
        {
            _current = image;
            _source = new MagickImage(_current.ToByteArray());
        }

        public void Resize(int width, int height)
        {
            _current.InterpolativeResize(width, height, PixelInterpolateMethod.Mesh);
        }

        public void Reset()
        {
            _current.Dispose();
            _current = new MagickImage(_source.ToByteArray());
        }

        public void SaveAsSource()
        {
            _source.Dispose();
            _source = new MagickImage(_current.ToByteArray());
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
            var ai = new MagickImage(_current.ToByteArray());
            return new AImage(ai);
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
            _current.AdaptiveBlur();
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
        public void BlurBoxes(BBOXES boxes)
        {
            foreach (var rect in boxes.ToMagikGeometry())
            {
                if (rect.X >= 0 && rect.Y >= 0 && rect.Width > 0 && rect.Height > 0)
                {
                    using (var crop_clone = new MagickImage(_current.ToByteArray()))
                    {
                        crop_clone.Crop(rect);
                        crop_clone.Blur();
                        _current.Composite(crop_clone, rect.X, rect.Y, CompositeOperator.Over);
                    }
                }
            }
        }

        public void LineNoize()
        {
            var rnd = new Random((int)Environment.TickCount);
            var count = rnd.Next(6 * Math.Max(_current.Width, _current.Height));
            for (int i = 0; i < count; i++)
            {
                var x1 = rnd.Next(0, _current.Width);
                var y1 = rnd.Next(0, _current.Height);
                var x2 = rnd.Next(-25, 25);
                var y2 = rnd.Next(-25, 25);
                byte[] rgb = new byte[3];
                rnd.NextBytes(rgb);
                DrawableFillColor fillColor = new DrawableFillColor(MagickColor.FromRgb(rgb[0], rgb[1], rgb[2]));
                DrawableLine dl = new DrawableLine(x1, y1, x1 + x2, y1 + y2);
                _current.Draw(fillColor, dl);
            }
        }

        public void FSin()
        {
            ushort max = 0;
            var const_multiplier = 2.0f * Math.PI / 255.0f;
            var pixels = this._current.GetPixels();
            var channels = pixels.Channels;
            for (var i = 0; i < this._current.Width; i++)
            {
                for (var j = 0; j < this._current.Height; j++)
                {
                    for (var c = 0; c < channels; c++)
                    {
                        pixels[i, j][c] = (ushort)((.5f * Math.Sin(const_multiplier * ((double)pixels[i, j][c] / 255.0f)) + .5f) * (double)pixels[i, j][c]);
                        if (pixels[i, j][c] > max) max = pixels[i, j][c];
                    }
                }
            }
            if (max < 255 * 255)
            {
                var ext = 255.0f * 255.0f / (float)max;
                for (var i = 0; i < this._current.Width; i++)
                {
                    for (var j = 0; j < this._current.Height; j++)
                    {
                        for (var c = 0; c < channels; c++)
                        {
                            pixels[i, j][c] = (ushort)(pixels[i, j][c] * ext);
                        }
                    }
                }
            }
            pixels.SetPixel(pixels);
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
