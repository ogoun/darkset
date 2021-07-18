using Darknet.Dataset.Merger.Helpers;
using Darknet.Dataset.Merger.Model;
using Darknet.Dataset.Merger.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using ZeroLevel;

namespace Darknet.Dataset.Merger.ViewModel
{
    public class MakeDatasetContext
        : BaseViewModel
    {
        #region Commands
        private ICommand _appendClassCommand;
        private ICommand _setDefaultClassCommand;
        private ICommand _removeClassCommandCommand;
        private ICommand _removeBoxCommand;
        private ICommand _changeBoxClassCommand;
        private ICommand _generateCommand;
        private ICommand _removeImageCommand;

        public ICommand AppendClassCommand => _appendClassCommand;
        public ICommand SetDefaultClassCommand => _setDefaultClassCommand;
        public ICommand RemoveClassCommandCommand => _removeClassCommandCommand;
        public ICommand RemoveBoxCommand => _removeBoxCommand;
        public ICommand ChangeBoxClassCommand => _changeBoxClassCommand;
        public ICommand GenerateCommand => _generateCommand;
        public ICommand RemoveImageCommand => _removeImageCommand;


        private void AppendClass(object state)
        {
            var className = state as string;
            if (!string.IsNullOrWhiteSpace(className))
            {
                if (!_classes.Contains(className))
                {
                    _classes.Add(className);
                    SaveClasses();
                    OnPropertyChanged("Classes");
                }
            }
        }

        private void SetDefaultClass(object state)
        {
            var className = state as string;
            if (!string.IsNullOrWhiteSpace(className))
            {
                _defaultClass = className;
                OnPropertyChanged("ClassTitle");
            }
        }

        private void RemoveClass(object state)
        {
            var className = state as string;
            if (!string.IsNullOrWhiteSpace(className))
            {
                if (_classes.Contains(className))
                {
                    if (_defaultClass?.Equals(className, StringComparison.Ordinal) ?? false)
                    {
                        _defaultClass = null;
                        OnPropertyChanged("ClassTitle");
                    }
                    _classes.Remove(className);
                    SaveClasses();
                    OnPropertyChanged("Classes");

                    var to_remove = new List<Annotation>();
                    foreach (var image in _images)
                    {
                        to_remove.Clear();
                        foreach (var a in image.Annotations)
                        {
                            if (string.Equals(className, a.Label))
                            {
                                to_remove.Add(a);
                            }
                        }
                        foreach (var a in to_remove) image.RemoveAnnotations(a);
                    }
                    UpdateBBoxes();
                }
            }
        }

        private void RemoveBox(object state)
        {
            var bbox = state as Annotation;
            if (bbox != null && _currentImage != null)
            {
                _currentImage.RemoveAnnotations(bbox);
                UpdateBBoxes();
            }
        }

        private void ChangeBoxClass(object state)
        {
            var bbox = state as Annotation;
            if (bbox != null && _currentImage != null)
            {
                var sw = new ClassSelectionWindow(_classes.ToList());
                if (sw.ShowDialog() == true)
                {
                    bbox.Label = sw.SelectedClass;
                    bbox.Class = _classes.IndexOf(bbox.Label);
                }
                UpdateBBoxes();
            }
        }

        public void Generate(object state)
        {
            // Store sets
            int test_step = 10;
            var trainData = new StringBuilder();
            var testData = new StringBuilder();
            for (int i = 0; i < _images.Count; i++)
            {
                var lp = Path.Combine(Path.GetDirectoryName(_images[i].FilePath), Path.GetFileNameWithoutExtension(_images[i].FilePath) + ".txt");
                if (!File.Exists(lp))
                {
                    File.WriteAllText(lp, string.Empty);
                }
                if (i != 0 && i % test_step == 0)
                {
                    testData.Append(_images[i].FilePath);
                    testData.Append("\n");
                }
                else
                {
                    trainData.Append(_images[i].FilePath);
                    trainData.Append("\n");
                }
            }
            File.WriteAllText(Path.Combine(_rootFolder, "train.txt"), trainData.ToString());
            File.WriteAllText(Path.Combine(_rootFolder, "test.txt"), testData.ToString());
            // Store data
            var dataInfo = new StringBuilder();
            dataInfo.Append($"classes= {_classes.Count}");
            dataInfo.Append("\n");
            dataInfo.Append($"train= {Path.Combine(_rootFolder, "train.txt")}");
            dataInfo.Append("\n");
            dataInfo.Append($"valid= {Path.Combine(_rootFolder, "test.txt")}");
            dataInfo.Append("\n");
            dataInfo.Append($"names= {Path.Combine(_rootFolder, "obj.names")}");
            dataInfo.Append("\n");
            dataInfo.Append($"backup= backup/");
            File.WriteAllText(Path.Combine(_rootFolder, "obj.data"), dataInfo.ToString());
        }

        private void RemoveImage(object state)
        {
            if (_currentImage != null)
            {
                _images.Remove(_currentImage);
                File.Delete(_currentImage.FilePath);
                var lp = Path.Combine(Path.GetDirectoryName(_currentImage.FilePath), Path.GetFileNameWithoutExtension(_currentImage.FilePath) + ".txt");
                if (File.Exists(lp))
                {
                    File.Delete(lp);
                }
                _currentImage = null;
            }
        }
        #endregion

        #region Storage
        private void SaveClasses()
        {
            File.WriteAllLines(Path.Combine(_rootFolder, "obj.names"), _classes);
            Injector.Default.SaveOrUpdate("classes", _classes.ToList());
        }

        private void LoadClasses()
        {
            var classesFile = Path.Combine(_rootFolder, "obj.names");
            if (File.Exists(classesFile))
            {
                _classes.Clear();
                foreach (var cn in File.ReadAllLines(Path.Combine(_rootFolder, "obj.names")))
                    _classes.Add(cn);
                OnPropertyChanged("Classes");
            }
            Injector.Default.SaveOrUpdate("classes", _classes.ToList());
        }
        #endregion

        private string _defaultClass;
        private ObservableCollection<string> _classes = new ObservableCollection<string>();
        private ObservableCollection<ImageInfo> _images = new ObservableCollection<ImageInfo>();
        private ObservableCollection<Annotation> _bboxes = new ObservableCollection<Annotation>();

        public string CurrentFolder { get; set; }

        private string _rootFolder;
        private ImageSource _viewImage;
        private ImageInfo _currentImage;

        #region Views
        public ImageSource ViewImage { get { return _viewImage; } set { _viewImage = value; OnPropertyChanged("ViewImage"); } }
        public ObservableCollection<string> Classes { get { return _classes; } }
        public ObservableCollection<ImageInfo> Images { get { return _images; } }
        public string ClassTitle { get { return "Classes" + (string.IsNullOrWhiteSpace(_defaultClass) ? string.Empty : $" (default: {_defaultClass})"); } }
        #endregion

        public ImageInfo SelectedImage { get { return _currentImage; } set { _currentImage = value; OnPropertyChanged("SelectedImage"); } }

        public ObservableCollection<Annotation> BBoxes { get { return _bboxes; } }

        public MakeDatasetContext()
        {
            _appendClassCommand = new RelayCommand(_ => true, AppendClass);
            _setDefaultClassCommand = new RelayCommand(_ => true, SetDefaultClass);
            _removeClassCommandCommand = new RelayCommand(_ => true, RemoveClass);
            _removeBoxCommand = new RelayCommand(_ => true, RemoveBox);
            _changeBoxClassCommand = new RelayCommand(_ => true, ChangeBoxClass);
            _generateCommand = new RelayCommand(_ => true, Generate);
            _removeImageCommand = new RelayCommand(_ => true, RemoveImage);
        }

        public void SetFolder(string folder)
        {
            _rootFolder = folder;
            AnalizeFolder(folder);
            CurrentFolder = Path.GetFileName(folder);
            OnPropertyChanged("CurrentFolder");
        }

        public void UpdateBBoxes()
        {
            _bboxes.Clear();
            if (_currentImage != null)
            {
                var sb = new StringBuilder();
                var to_remove = new List<Annotation>();
                _bboxes.Add(new Annotation { Class = -1, Label = "-- ALL --", Cx = -1, Cy = -1, Width = 0, Height = 0 });
                foreach (var a in _currentImage.Annotations)
                {
                    if (string.IsNullOrWhiteSpace(a.Label))
                    {
                        if (!string.IsNullOrWhiteSpace(_defaultClass))
                        {
                            a.Class = _classes.IndexOf(_defaultClass);
                            a.Label = _defaultClass;
                        }
                        else
                        {
                            var sw = new ClassSelectionWindow(_classes.ToList());
                            if (sw.ShowDialog() == true)
                            {
                                a.Label = sw.SelectedClass;
                                a.Class = _classes.IndexOf(a.Label);
                            }
                            else
                            {
                                to_remove.Add(a);
                                continue;
                            }
                        }
                    }
                    _bboxes.Add(a);
                    sb.Append($"{a.Class} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                    sb.Append("\n");
                }
                foreach (var a in to_remove)
                {
                    _currentImage.RemoveAnnotations(a);
                }
                var lp = Path.Combine(Path.GetDirectoryName(_currentImage.FilePath), Path.GetFileNameWithoutExtension(_currentImage.FilePath) + ".txt");
                File.WriteAllText(lp, sb.ToString());
            }
            OnPropertyChanged("BBoxes");
        }

        public void SetCurrentImage(ImageInfo image)
        {
            SelectedImage = image;
            if (image != null)
            {
                using (var bmp = new Bitmap(image.FilePath).ToARGBBitmap())
                {
                    ViewImage = BitmapSourceHelper.LoadBitmap(bmp);
                }
                _bboxes.Clear();
                _bboxes.Add(new Annotation { Class = -1, Label = "-- ALL --", Cx = -1, Cy = -1, Width = 0, Height = 0 });
                foreach (var a in image.Annotations)
                {
                    _bboxes.Add(a);
                }
                OnPropertyChanged("BBoxes");
            }
        }

        private void AnalizeFolder(string folder)
        {
            var objFolder = Path.Combine(folder, "obj");
            var filelist = new List<string>();
            if (Directory.Exists(objFolder))
            {
                filelist.AddRange(Directory.GetFiles(objFolder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
                if (filelist.Count == 0)
                {
                    filelist.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
                    if (filelist.Count == 0)
                    {
                        throw new Exception("Not found any images");
                    }
                    else
                    {
                        filelist = TransferImagesToObjFolder(filelist, objFolder);
                    }
                }
            }
            else
            {
                filelist.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
                    || s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)));
                if (filelist.Count == 0)
                {
                    throw new Exception("Not found any images");
                }
                else
                {
                    Directory.CreateDirectory(objFolder);
                    filelist = TransferImagesToObjFolder(filelist, objFolder);
                }
            }
            LoadClasses();
            foreach (var file in filelist)
            {
                try
                {
                    var lp = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".txt");
                    var a = new List<Annotation>();
                    if (File.Exists(lp))
                    {
                        int classNumber;
                        float cx, cy, w, h;
                        foreach (var line in File.ReadAllLines(lp))
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
                    }
                    _images.Add(new ImageInfo(file, ImageTrainType.Train, a));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Fault handle image '{file}'");
                }
            }
        }

        private List<string> TransferImagesToObjFolder(List<string> images, string objFolder)
        {
            return images.Select(image =>
            {
                var to_path = Path.Combine(objFolder, Path.GetFileName(image));
                File.Move(image, to_path);
                return to_path;
            }).ToList();
        }
    }
}
