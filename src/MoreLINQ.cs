using System.Collections.Generic;

namespace System.Linq
{
    public static class MoreLINQ
    {
        public static void ThrowIfNull<T>(this T val, string message = null)
        {
            if (null == val)
                throw new ArgumentNullException(message);
        }

        public static void Apply<T>(this IEnumerable<T> seq, Action<T> action)
        {
            seq.ThrowIfNull();
            action.ThrowIfNull();
            foreach (var e in seq)
            {
                action.Invoke(e);
            }
        }
    }
}
