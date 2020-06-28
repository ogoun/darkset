using Darknet.Dataset.Merger.Helpers;
using Darknet.Dataset.Merger.Model;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Media;

namespace Darknet.Dataset.Merger.ViewModel
{
    public class DatasetViewerContext
        : BaseViewModel
    {
        private readonly Dataset _dataset;

        public IReadOnlyList<string> Classes => _dataset.Classes;
        public IReadOnlyList<ImageInfo> Images => _dataset.Images;
        public IReadOnlyCollection<ImageInfo> TrainImages => _dataset.Images.Where(i => i.TrainType != ImageTrainType.Test).ToList();
        public IReadOnlyCollection<ImageInfo> ValidImages => _dataset.Images.Where(i => i.TrainType == ImageTrainType.Test).ToList();

        private readonly System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Brushes.Red, 4.0f);

        private ImageSource _currentImage;
        public ImageSource CurrentImage { get { return _currentImage; } set { _currentImage = value; OnPropertyChanged("CurrentImage"); } }

        public DatasetViewerContext(Dataset dataset)
        {
            _dataset = dataset;
        }

        public void SetCurrentImage(ImageInfo image)
        {
            using (var bmp = new Bitmap(image.FilePath))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    foreach (var a in image.Annotations)
                    {
                        var x = (int)(bmp.Width * (a.Cx - a.Width / 2));
                        var y = (int)(bmp.Height * (a.Cy - a.Height / 2));
                        var w = (int)(bmp.Width * a.Width);
                        var h = (int)(bmp.Height * a.Height);

                        g.DrawRectangle(pen, x, y, w, h);
                    }
                }
                CurrentImage = BitmapSourceHelper.LoadBitmap(bmp);
            }
        }
    }
}
