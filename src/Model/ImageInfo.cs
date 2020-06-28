using Darknet.Dataset.Merger.Helpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace Darknet.Dataset.Merger.Model
{
    public class ImageInfo
         : BaseViewModel
    {
        private string _filePath;
        private ImageTrainType _trainType;
        private List<Annotation> _annotations;

        public bool HasAnnotations { get { return _annotations != null && _annotations.Count > 0; } }
        public string Name { get; }
        public string FilePath => _filePath;
        public ImageTrainType TrainType => _trainType;
        public IReadOnlyList<Annotation> Annotations => _annotations;

        public void AddAnnotation(Annotation annotation)
        {
            bool has_ann_event = _annotations.Count == 0;
            _annotations.Add(annotation);
            OnPropertyChanged("Annotations");
            if (has_ann_event)
            {
                OnPropertyChanged("HasAnnotations");
            }
        }

        public void RemoveAnnotations(Annotation annotation)
        {
            bool has_ann_event = _annotations.Count != 0;
            _annotations.Remove(annotation);
            OnPropertyChanged("Annotations");
            if (has_ann_event && _annotations.Count == 0)
            {
                OnPropertyChanged("HasAnnotations");
            }
        }

        public ImageInfo(string filePath, ImageTrainType trainType, IEnumerable<Annotation> annotations = null)
        {
            Name = $"[{trainType.ToString()}] {Path.GetFileNameWithoutExtension(filePath)}";
            _filePath = filePath;
            _trainType = trainType;
            _annotations = new List<Annotation>();
            if (annotations != null)
            {
                _annotations.AddRange(annotations);
            }
        }

        private static Annotation Yacobi(Annotation source, float imageWidth, float imageHeight, float boxX, float boxY, float boxW, float boxH)
        {
            var ax = (source.Cx - (source.Width / 2.0f)) * imageWidth;
            var ay = (source.Cy - (source.Height / 2.0f)) * imageHeight;
            var aw = source.Width * imageWidth;
            var ah = source.Height * imageHeight;

            if (ax < boxX || (ax + aw) > (boxX + boxW)) return null;
            if (ay < boxY || (ay + ah) > (boxY + boxH)) return null;

            var xmin = Math.Max(ax, boxX) - boxX;
            var ymin = Math.Max(ay, boxY) - boxY;
            var xmax = Math.Min(ax + aw, boxX + boxW) - boxX;
            var ymax = Math.Min(ay + ah, boxY + boxH) - boxY;

            var w = xmax - xmin;
            var h = ymax - ymin;

            if (w < 10 || h < 10) return null;

            return new Annotation
            {
                Class = source.Class,
                Label = source.Label,
                Cx = (xmin + w / 2.0f) / boxW,
                Cy = (ymin + h / 2.0f) / boxH,
                Width = w / boxW,
                Height = h / boxH
            };
        }

        public IEnumerable<ImageFragment> SplitTo(int xParts, int yParts, bool appendOverrides)
        {
            var size = ImageHelper.GetDimensions(FilePath);
            var fw = size.Width / xParts;
            var fh = size.Height / yParts;

            for (int xi = 0; xi < xParts; xi++)
            {
                int x = xi * fw;
                for (int yi = 0; yi < yParts; yi++)
                {
                    int y = yi * fh;
                    int width = fw;
                    int height = fh;

                    if ((x + width) > size.Width)
                    {
                        width = size.Width - x;
                    }
                    if ((y + height) > size.Height)
                    {
                        height = size.Height - y;
                    }

                    var annotations = new List<Annotation>();
                    foreach (var annotation in Annotations)
                    {
                        var a = Yacobi(annotation, size.Width, size.Height, x, y, width, height);
                        if (a != null)
                            annotations.Add(a);
                    }
                    yield return new ImageFragment
                    {
                        Annotations = annotations,
                        FilePath = this.FilePath,
                        X = x,
                        Y = y,
                        Height = height,
                        Width = width
                    };
                }
            }

            if (appendOverrides)
            {
                for (int xi = 0; xi < xParts - 1; xi++)
                {
                    int x = xi * fw / 2;
                    for (int yi = 0; yi < yParts; yi++)
                    {
                        int y = yi * fh;
                        int width = fw;
                        int height = fh;

                        if ((x + width) > size.Width)
                        {
                            width = size.Width - x;
                        }
                        if ((y + height) > size.Height)
                        {
                            height = size.Height - y;
                        }

                        var annotations = new List<Annotation>();
                        foreach (var annotation in Annotations)
                        {
                            var a = Yacobi(annotation, size.Width, size.Height, x, y, width, height);
                            if (a != null)
                                annotations.Add(a);
                        }
                        yield return new ImageFragment
                        {
                            Annotations = annotations,
                            FilePath = this.FilePath,
                            X = x,
                            Y = y,
                            Height = height,
                            Width = width
                        };
                    }

                    for (int yi = 0; yi < yParts - 1; yi++)
                    {
                        int y = yi * fh / 2;
                        int width = fw;
                        int height = fh;

                        if ((x + width) > size.Width)
                        {
                            width = size.Width - x;
                        }
                        if ((y + height) > size.Height)
                        {
                            height = size.Height - y;
                        }
                        var annotations = new List<Annotation>();
                        foreach (var annotation in Annotations)
                        {
                            var a = Yacobi(annotation, size.Width, size.Height, x, y, width, height);
                            if (a != null)
                                annotations.Add(a);
                        }
                        yield return new ImageFragment
                        {
                            Annotations = annotations,
                            FilePath = this.FilePath,
                            X = x,
                            Y = y,
                            Height = height,
                            Width = width
                        };
                    }
                }
            }
        }

        public IEnumerable<ImageFragment> CutTo(int cutWidth, int cutHeight, bool appendOverrides)
        {
            var size = ImageHelper.GetDimensions(FilePath);
            if (size.Width < cutWidth || size.Height < cutHeight)
            {
                yield break;
            }
            var x_offset = appendOverrides ? cutWidth / 2 : cutWidth;
            var y_offset = appendOverrides ? cutHeight / 2 : cutHeight;
            for (var j = 0.0f; (j + cutHeight) < size.Height; j += y_offset)
            {
                for (var i = 0.0f; (i + cutWidth) < size.Width; i += x_offset)
                {
                    var left = i;
                    var top = j;
                    var dr = (left + cutWidth) - size.Width;
                    if (dr > 0)
                    {
                        left = left - dr;
                    }
                    var db = (top + cutHeight) - size.Height;
                    if (db > 0)
                    {
                        top = top - db;
                    }
                    var annotations = new List<Annotation>();
                    foreach (var annotation in Annotations)
                    {
                        var a = Yacobi(annotation, size.Width, size.Height, left, top, cutWidth, cutHeight);
                        if (a != null)
                            annotations.Add(a);
                    }
                    yield return new ImageFragment
                    {
                        Annotations = annotations,
                        FilePath = this.FilePath,
                        X = (int)left,
                        Y = (int)top,
                        Height = cutHeight,
                        Width = cutWidth
                    };
                }
            }
        }
    }
}
