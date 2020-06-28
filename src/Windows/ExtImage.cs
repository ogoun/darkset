using Darknet.Dataset.Merger.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Darknet.Dataset.Merger.Windows
{
    public class ExtImage
        : Image
    {
        Point? _bboxStartPoint = null;
        Point? _bboxEndPoint = null;
        private readonly Pen _fillPen;
        private readonly Pen _bboxPen;

        private ImageInfo _selectedImage;

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

            _bboxPen = new Pen(new SolidColorBrush(Colors.Lime), 2);
            _bboxPen.DashStyle = DashStyles.Solid;
        }

        public void SetStartBorderPoint(Point? start)
        {
            _bboxStartPoint = start;
        }

        public void SetCurrentBorderPoint(Point? point)
        {
            _bboxEndPoint = point;
        }

        public void ResetBorderPoints()
        {
            GetBBox();
            _bboxStartPoint = _bboxEndPoint = null;
        }

        public void GetBBox()
        {
            if (_bboxStartPoint != null && _bboxEndPoint != null && _selectedImage!=null)
            {
                var left = Math.Min(_bboxStartPoint.Value.X, _bboxEndPoint.Value.X);
                var right = Math.Max(_bboxStartPoint.Value.X, _bboxEndPoint.Value.X);
                var top = Math.Min(_bboxStartPoint.Value.Y, _bboxEndPoint.Value.Y);
                var bottom = Math.Max(_bboxStartPoint.Value.Y, _bboxEndPoint.Value.Y);
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

            if (_selectedImage!=null && _selectedImage.Annotations.Count > 0)
            {
                foreach (var bbox in _selectedImage.Annotations)
                {
                    var left = bbox.Cx - bbox.Width / 2.0f;
                    var top = bbox.Cy - bbox.Height / 2.0f;
                    var r = new Rect(left * ActualWidth, top * ActualHeight, bbox.Width * ActualWidth, bbox.Height * ActualHeight);
                    drawingContext.DrawRectangle(null, _bboxPen, r);
                }
            }
        }
    }
}
