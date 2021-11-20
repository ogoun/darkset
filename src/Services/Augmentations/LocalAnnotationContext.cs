using Darknet.Dataset.Merger.Model;
using System.Collections.Generic;

namespace Darknet.Dataset.Merger.Services
{
    public class LocalAnnotationContext
    {
        public ImageInfo Image { get; set; }
        public IEnumerable<Annotation> Annotation { get; set; }
    }
}
