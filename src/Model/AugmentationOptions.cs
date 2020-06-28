namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationOptions
    {
        public bool Grayscale { get; set; } = true;
        public bool Brightness { get; set; } = true;
        public bool Contrast { get; set; } = true;
        public bool RGBDiffs { get; set; } = true;

        public bool Rotation { get; set; } = true;
        public bool Mirrors { get; set; } = true;
        public bool Stretching { get; set; } = true;
        public bool Shifts { get; set; } = true;

        public bool Invert { get; set; } = true;
        public bool Gotham { get; set; } = true;
        public bool Sepia { get; set; } = true;
        public bool Blur { get; set; } = true;

        public bool BBoxRotation { get; set; } = true;
        public bool BBoxShifts { get; set; } = true;
        public bool BBoxStretching { get; set; } = true;
        public bool BBoxMirrors { get; set; } = true;

        public bool Cut { get; set; } = true;
        public bool CutOverlaps { get; set; } = true;
        public int CutWidth { get; set; } = 1920;
        public int CutHeight { get; set; } = 1080;
    }
}
