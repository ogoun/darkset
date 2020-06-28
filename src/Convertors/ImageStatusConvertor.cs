using System;
using System.Globalization;
using System.Windows.Media;

namespace Darknet.Dataset.Merger.Convertors
{
    public class ImageStatusConvertor
        : BaseConvertor<ImageStatusConvertor>
    {
        private readonly SolidColorBrush _hasAnnotationColor;
        private readonly SolidColorBrush _hasNotAnnotationsColor;

        public ImageStatusConvertor()
        {
            _hasNotAnnotationsColor = new SolidColorBrush(System.Windows.Media.Colors.DarkRed);
            _hasAnnotationColor = new SolidColorBrush(System.Windows.Media.Colors.LightGreen);
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = (bool)value;
            return b ? _hasAnnotationColor : _hasNotAnnotationsColor;
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
