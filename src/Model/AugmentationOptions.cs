namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationOptions
    {
        public bool Grayscale { get; set; } = false;
        public bool Sepia { get; set; } = false;
        public bool Blur { get; set; } = false;
        public bool Charcoal { get; set; } = false;
        public bool BBoxBlur { get; set; } = false;
        public bool LinesNoise { get; set; } = false;
        public bool WithoutClass { get; set; } = false;


        public bool HasAugmentation =>
            Grayscale || BBoxBlur || LinesNoise || 
            Sepia || Blur || Charcoal;

        /// <summary>
        /// Резать оригинальное изображение на части
        /// </summary>
        public bool Cut { get; set; } = false;
        /// <summary>
        /// Разрезать на части с перекрытиями
        /// </summary>
        public bool CutOverlaps { get; set; } = true;
        /// <summary>
        /// Ширина фрагмента
        /// </summary>
        public int CutWidth { get; set; } = 1280;
        /// <summary>
        /// Высота фрагмента
        /// </summary>
        public int CutHeight { get; set; } = 1280;


        public bool ResizeToInput { get; set; } = false;
        /// <summary>
        /// Ширина входа
        /// </summary>
        public int InputWidth { get; set; } = 640;
        /// <summary>
        /// Высота входа
        /// </summary>
        public int InputHeight { get; set; } = 640;
    }
}
