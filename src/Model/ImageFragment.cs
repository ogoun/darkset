using System.Collections.Generic;

namespace Darknet.Dataset.Merger.Model
{
    public class ImageFragment
    {
        public string FilePath { get; set; }

        public int X;
        public int Y;
        public int Width;
        public int Height;
        public IEnumerable<Annotation> Annotations { get; set; }
    }
}
