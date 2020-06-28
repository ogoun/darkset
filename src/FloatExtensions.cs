using System.Globalization;

namespace Darknet.Dataset.Merger
{
    public static class FloatExtensions
    {
        public static float TryConvertToFloat(this string line)
        {
            var s = line.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            float f;
            if (float.TryParse(s, out f)) return f;
            return float.NaN;
        }

        public static string ConvertToString(this float num)
        {
            return num.ToString().Replace(',', '.');
        }
    }
}
