namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationOptions
    {
        public bool Grayscale { get; set; } = false;
        public bool Noise { get; set; } = false;
        public bool BBoxBlur { get; set; } = false;
        public bool LinesNoise { get; set; } = false;

        public bool Mirrors { get; set; } = false;
        public bool Sepia { get; set; } = false;
        public bool Moonlight { get; set; } = false;
        public bool Blur { get; set; } = false;

        public bool Charcoal { get; set; } = false;
        public bool Sin { get; set; } = false;


        public bool HasAugmentation =>
            Grayscale || Noise || BBoxBlur || LinesNoise || Sin ||
            Mirrors || Sepia || Moonlight || Blur || Charcoal;

        /// <summary>
        /// Менять оригинальный размер на более подходящий входу нейросети
        /// </summary>
        public bool ResizeOriginal { get; set; } = false;
        /// <summary>
        /// Резать оригинальное изображение на части
        /// </summary>
        public bool Cut { get; set; } = false;
        /// <summary>
        /// Разрезать на части с перекрытиями
        /// </summary>
        public bool CutOverlaps { get; set; } = true;
        /// <summary>
        /// Ширина фрагмнта
        /// </summary>
        public int CutWidth { get; set; } = 1920;
        /// <summary>
        /// Высота фрагмнта
        /// </summary>
        public int CutHeight { get; set; } = 1920;
    }
}
