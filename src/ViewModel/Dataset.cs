using Darknet.Dataset.Merger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZeroLevel;

namespace Darknet.Dataset.Merger
{
    public class Dataset
        : BaseViewModel
    {
        private string _datasetPath;
        private List<string> _classes;
        private HashSet<string> _trainImages;
        private HashSet<string> _validImages;
        private List<ImageInfo> _images;

        public string DatasetPath
        {
            get { return _datasetPath; }
            set { _datasetPath = value; OnPropertyChanged("DatasetPath"); }
        }

        public AugmentationOptions Augmentations = new AugmentationOptions();
        public IReadOnlyList<string> Classes => _classes;
        public IReadOnlyList<ImageInfo> Images => _images;
        public IReadOnlyCollection<string> TrainImages => _trainImages;
        public IReadOnlyCollection<string> ValidImages => _validImages;

        public Dataset(string path)
        {
            DatasetPath = path;
            LoadDataset();
        }

        private void LoadDataset()
        {
            var dataInfo = File
                .ReadAllLines(System.IO.Path.Combine(_datasetPath, "obj.data"))
                .Select(line => line.Split('='))
                .Where(a => a.Length == 2)
                .ToDictionary(a => a[0].Trim(), a => a[1].Trim());

            string trainImagesFilePath, validImagesFilePath, namesFilePath;

            if (System.IO.Path.IsPathRooted(dataInfo["train"]))
            {
                trainImagesFilePath = dataInfo["train"];
            }
            else
            {
                trainImagesFilePath = System.IO.Path.Combine(_datasetPath, dataInfo["train"]);
            }
            if (System.IO.Path.IsPathRooted(dataInfo["valid"]))
            {
                validImagesFilePath = dataInfo["valid"];
            }
            else
            {
                validImagesFilePath = System.IO.Path.Combine(_datasetPath, dataInfo["valid"]);
            }
            if (System.IO.Path.IsPathRooted(dataInfo["names"]))
            {
                namesFilePath = dataInfo["names"];
            }
            else
            {
                namesFilePath = System.IO.Path.Combine(_datasetPath, dataInfo["names"]);
            }

            _classes = new List<string>(File.ReadAllLines(namesFilePath));
            _trainImages = new HashSet<string>(File.ReadAllLines(trainImagesFilePath));
            _validImages = new HashSet<string>(File.ReadAllLines(validImagesFilePath));
            _images = new List<ImageInfo>();

            var appendFunc = new Action<string, IEnumerable<string>, ImageTrainType>((root, images, type) =>
            {
                int classNumber;
                float cx, cy, w, h;
                foreach (var file in images)
                {
                    var fp = file;
                    if (!File.Exists(fp))
                    {
                        fp = Path.Combine(root, "obj", Path.GetFileName(file));
                    }
                    if (File.Exists(fp))
                    {
                        var folder = Path.GetDirectoryName(fp);
                        var name = Path.GetFileNameWithoutExtension(fp);
                        var labelFile = Path.Combine(folder, name + ".txt");
                        if (File.Exists(labelFile))
                        {
                            var a = new List<Annotation>();
                            foreach (var line in File.ReadAllLines(labelFile))
                            {
                                var parts = line.Split(' ').Where(s => string.IsNullOrWhiteSpace(s) == false).ToArray();
                                if (int.TryParse(parts[0], out classNumber) == false) continue;
                                cx = parts[1].TryConvertToFloat();
                                cy = parts[2].TryConvertToFloat();
                                w = parts[3].TryConvertToFloat();
                                h = parts[4].TryConvertToFloat();
                                if (float.IsNaN(cx) || float.IsNaN(cy) || float.IsNaN(w) || float.IsNaN(h)) continue;
                                a.Add(new Annotation
                                {
                                    Class = classNumber,
                                    Cx = cx,
                                    Cy = cy,
                                    Width = w,
                                    Height = h,
                                    Label = _classes.Count > classNumber && classNumber >= 0 ? _classes[classNumber] : classNumber.ToString()
                                });
                            }
                            _images.Add(new ImageInfo(fp, type, a));
                        }
                        else
                        {
                            Log.Warning($"File {labelFile} missed");
                        }
                    }
                    else
                    {
                        Log.Warning($"File {file} missed");
                    }
                }
            });

            appendFunc(_datasetPath, _trainImages, ImageTrainType.Train);
            appendFunc(_datasetPath, _validImages, ImageTrainType.Test);
        }
    }
}
