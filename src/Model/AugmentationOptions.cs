namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationOptions
    {
        public bool Grayscale { get; set; } = true;
        public bool Brightness { get; set; } = false;
        public bool Contrast { get; set; } = false;
        public bool Moonlight { get; set; } = false;

        public bool Rotation { get; set; } = false;
        public bool Mirrors { get; set; } = false;
        public bool Stretching { get; set; } = false;
        public bool Shifts { get; set; } = false;

        public bool Noise { get; set; } = true;
        public bool Charcoal { get; set; } = false;
        public bool Sepia { get; set; } = false;
        public bool Blur { get; set; } = false;

        public bool BBoxRotation { get; set; } = true;
        public bool BBoxBlur { get; set; } = true;
        public bool BBoxStretching { get; set; } = true;
        public bool BBoxMirrors { get; set; } = true;

        public bool Cut { get; set; } = false;
        public bool CutOverlaps { get; set; } = true;
        public int CutWidth { get; set; } = 1920;
        public int CutHeight { get; set; } = 1920;
    }
}
