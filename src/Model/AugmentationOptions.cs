namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationOptions
    {
        public bool Grayscale { get; set; } = true;
        public bool Mirrors { get; set; } = false;
        public bool Noise { get; set; } = true;
        public bool Sepia { get; set; } = false;

        public bool Moonlight { get; set; } = false;
        public bool Blur { get; set; } = false;
        public bool BBoxBlur { get; set; } = true;
        public bool Charcoal { get; set; } = false;

        public bool LinesNoise { get; set; } = false;
        public bool Sin { get; set; } = false;

        public bool Cut { get; set; } = false;
        public bool CutOverlaps { get; set; } = true;
        public int CutWidth { get; set; } = 1920;
        public int CutHeight { get; set; } = 1920;
    }
}
