using Darknet.Dataset.Merger.Model;

namespace Darknet.Dataset.Merger.Services
{
    public class LocalAnnotationContext
    {
        public ImageInfo Image { get; set; }
        public string SourceAnnotationText { get; set; }
    }
}
