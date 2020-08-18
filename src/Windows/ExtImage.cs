using Darknet.Dataset.Merger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroLevel;

namespace Darknet.Dataset.Merger.Windows
{
    public class ExtImage
        : Image
    {
        Point? _bboxStartPoint = null;
        Point? _bboxEndPoint = null;
        private readonly Pen _fillPen;
        private readonly Pen _bboxPen;
        private readonly Pen _selectedBboxPen;

        private readonly Pen _bboxPen1;
        private readonly Pen _bboxPen2;
        private readonly Pen _bboxPen3;

        private ImageInfo _selectedImage;
        private Annotation _selectedBbox;

        public static readonly DependencyProperty SelectedImageProperty =
         DependencyProperty.Register("SelectedImage", typeof(ImageInfo), typeof(ExtImage), new
            PropertyMetadata(null, new PropertyChangedCallback(OnSelectedImage)));

        public ImageInfo SelectedImage
        {
            get { return (ImageInfo)GetValue(SelectedImageProperty); }
            set { SetValue(SelectedImageProperty, value); }
        }

        private static void OnSelectedImage(DependencyObject d,
           DependencyPropertyChangedEventArgs e)
        {
            var control = d as ExtImage;
            control.OnSelectedImage(e);
        }

        private void OnSelectedImage(DependencyPropertyChangedEventArgs e)
        {
            _selectedImage = e.NewValue as ImageInfo;
        }

        public ExtImage() : base()
        {
            _fillPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 10, 20)), 2);
            _fillPen.DashStyle = DashStyles.Dash;

            _bboxPen = new Pen(new SolidColorBrush(Colors.Black), 1);
            _bboxPen.DashStyle = DashStyles.Solid;
            _bboxPen1 = new Pen(new SolidColorBrush(Colors.White), 1);
            _bboxPen1.DashStyle = DashStyles.Solid;

            _bboxPen2 = new Pen(new SolidColorBrush(Colors.GreenYellow), 1);
            _bboxPen2.DashStyle = DashStyles.Solid;
            _bboxPen3 = new Pen(new SolidColorBrush(Colors.Lime), 1);
            _bboxPen3.DashStyle = DashStyles.Solid;

            _selectedBboxPen = new Pen(new SolidColorBrush(Colors.Red), 3);
            _selectedBboxPen.DashStyle = DashStyles.Solid;
        }

        public void SetStartBorderPoint(Point? start)
        {
            _bboxStartPoint = start;
        }

        public void SetCurrentBorderPoint(Point? point)
        {
            _bboxEndPoint = point;
        }

        public void RemoveBBox(Point? point)
        {
            if (point.HasValue)
            {
                var px = (float)((float)point.Value.X / ActualWidth);
                var py = (float)((float)point.Value.Y / ActualHeight);
                var to_remove = new List<Annotation>();
                foreach (var a in _selectedImage.Annotations)
                {
                    if (px > a.Left && px < a.Right
                        && py > a.Top && py < a.Bottom)
                    {
                        to_remove.Add(a);
                    }
                }
                foreach (var a in to_remove)
                {
                    _selectedImage.RemoveAnnotations(a);
                }
            }
        }

        public void ChangeClass(Point? point)
        {
            if (point.HasValue)
            {
                var classes = Injector.Default.Get<List<string>>("classes");
                var px = (float)((float)point.Value.X / ActualWidth);
                var py = (float)((float)point.Value.Y / ActualHeight);
                foreach (var a in _selectedImage.Annotations)
                {
                    if (px > a.Left && px < a.Right
                        && py > a.Top && py < a.Bottom)
                    {
                        var sw = new ClassSelectionWindow(classes);
                        if (sw.ShowDialog() == true)
                        {
                            a.Label = sw.SelectedClass;
                            a.Class = classes.IndexOf(a.Label);
                        }
                    }
                }
            }
        }

        public void ResetBorderPoints()
        {
            GetBBox();
            _bboxStartPoint = _bboxEndPoint = null;
        }

        public void SetSelectedBBox(Annotation bbox)
        {
            _selectedBbox = bbox;
        }

        public void GetBBox()
        {
            if (_bboxStartPoint != null && _bboxEndPoint != null && _selectedImage != null)
            {
                var left = Math.Min((float)_bboxStartPoint.Value.X, (float)_bboxEndPoint.Value.X);
                var right = Math.Max((float)_bboxStartPoint.Value.X, (float)_bboxEndPoint.Value.X);
                var top = Math.Min((float)_bboxStartPoint.Value.Y, (float)_bboxEndPoint.Value.Y);
                var bottom = Math.Max((float)_bboxStartPoint.Value.Y, (float)_bboxEndPoint.Value.Y);

                if (left < 0) left = 0;
                if (right > ActualWidth) right = (float)ActualWidth;
                if (top < 0) top = 0;
                if (bottom > ActualHeight) bottom = (float)ActualHeight;

                var width = right - left;
                var height = bottom - top;

                if ((width * height) > 0.001) // mouse false positives
                {
                    var a = new Annotation
                    {
                        Cx = (float)((left + width / 2.0d) / ActualWidth),
                        Cy = (float)((top + height / 2.0d) / ActualHeight),
                        Width = (float)(width / ActualWidth),
                        Height = (float)(height / ActualHeight)
                    };
                    _selectedImage.AddAnnotation(a);
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (_bboxStartPoint != null && _bboxEndPoint != null)
            {
                var left = Math.Min(_bboxStartPoint.Value.X, _bboxEndPoint.Value.X);
                var right = Math.Max(_bboxStartPoint.Value.X, _bboxEndPoint.Value.X);

                var top = Math.Min(_bboxStartPoint.Value.Y, _bboxEndPoint.Value.Y);
                var bottom = Math.Max(_bboxStartPoint.Value.Y, _bboxEndPoint.Value.Y);

                var width = right - left;
                var height = bottom - top;

                drawingContext.DrawRectangle(null, _fillPen, new Rect(left, top, width, height));
            }

            if (_selectedImage != null && _selectedImage.Annotations.Count > 0)
            {
                if (_selectedBbox != null)
                {
                    var left = _selectedBbox.Cx - _selectedBbox.Width / 2.0f;
                    var top = _selectedBbox.Cy - _selectedBbox.Height / 2.0f;
                    var r = new Rect(left * ActualWidth - 2, top * ActualHeight - 2, _selectedBbox.Width * ActualWidth + 4, _selectedBbox.Height * ActualHeight + 4);
                    drawingContext.DrawRectangle(null, _selectedBboxPen, r);
                }
                else
                {
                    foreach (var bbox in _selectedImage.Annotations)
                    {
                        var left = bbox.Cx - bbox.Width / 2.0f;
                        var top = bbox.Cy - bbox.Height / 2.0f;
                        var r1 = new Rect(left * ActualWidth, top * ActualHeight, bbox.Width * ActualWidth, bbox.Height * ActualHeight);
                        var r2 = new Rect(left * ActualWidth - 2, top * ActualHeight - 2, bbox.Width * ActualWidth + 4, bbox.Height * ActualHeight + 4);
                        var center = new Point(bbox.Cx * ActualWidth, bbox.Cy * ActualHeight);
                        var max_side = Math.Max(bbox.Width * ActualWidth, bbox.Height * ActualHeight) * 1.4f;

                        //drawingContext.DrawEllipse(null, _bboxPen, center, max_side, max_side);
                        //drawingContext.DrawEllipse(null, _bboxPen1, center, max_side + .5f, max_side + .5f);
                        drawingContext.DrawRectangle(null, _bboxPen2, r1);
                        //drawingContext.DrawRectangle(null, _bboxPen3, r2);
                    }
                }
                /*
                foreach (var bbox in _selectedImage.Annotations)
                {
                    var left = bbox.Cx - bbox.Width / 2.0f;
                    var top = bbox.Cy - bbox.Height / 2.0f;
                    var r1 = new Rect(left * ActualWidth, top * ActualHeight, bbox.Width * ActualWidth, bbox.Height * ActualHeight);
                    var r2 = new Rect(left * ActualWidth - 2, top * ActualHeight - 2, bbox.Width * ActualWidth + 4, bbox.Height * ActualHeight + 4);
                    var center = new Point(bbox.Cx * ActualWidth, bbox.Cy * ActualHeight);
                    var max_side = Math.Max(bbox.Width * ActualWidth, bbox.Height * ActualHeight) * 1.4f;

                    if (Object.ReferenceEquals(bbox, _selectedBbox))
                    {
                        drawingContext.DrawRectangle(null, _selectedBboxPen, r2);
                    }
                    else
                    {
                        drawingContext.DrawEllipse(null, _bboxPen, center, max_side, max_side);
                        drawingContext.DrawEllipse(null, _bboxPen1, center, max_side + .5f, max_side + .5f);

                        drawingContext.DrawRectangle(null, _bboxPen2, r1);
                        drawingContext.DrawRectangle(null, _bboxPen3, r2);
                    }
                }*/
            }
        }
    }
}
