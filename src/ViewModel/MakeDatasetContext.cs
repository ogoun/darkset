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

        public ICommand AppendClassCommand => _appendClassCommand;
        public ICommand SetDefaultClassCommand => _setDefaultClassCommand;
        public ICommand RemoveClassCommandCommand => _removeClassCommandCommand;
        public ICommand RemoveBoxCommand => _removeBoxCommand;

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
        #endregion

        #region Storage
        private void SaveClasses()
        {
            File.WriteAllLines(Path.Combine(_rootFolder, "obj.names"), _classes);
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
        }
        #endregion

        private string _defaultClass;
        private ObservableCollection<string> _classes = new ObservableCollection<string>();
        private ObservableCollection<ImageInfo> _images = new ObservableCollection<ImageInfo>();
        private ObservableCollection<Annotation> _bboxes = new ObservableCollection<Annotation>();

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
        }

        public void SetFolder(string folder)
        {
            _rootFolder = folder;
            AnalizeFolder(folder);
        }

        public void UpdateBBoxes()
        {
            _bboxes.Clear();
            if (_currentImage != null)
            {
                var sb = new StringBuilder();
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
                        }
                    }
                    _bboxes.Add(a);
                    sb.Append($"{a.Class} {a.Cx.ConvertToString()} {a.Cy.ConvertToString()} {a.Width.ConvertToString()} {a.Height.ConvertToString()}");
                    sb.Append("\n");
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
                using (var bmp = new Bitmap(image.FilePath))
                {
                    ViewImage = BitmapSourceHelper.LoadBitmap(bmp);
                }
                _bboxes.Clear();
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
                filelist.AddRange(Directory.GetFiles(objFolder, "*.*").Where(s => s.EndsWith(".jpeg") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".png")));
                if (filelist.Count == 0)
                {
                    filelist.AddRange(Directory.GetFiles(folder, "*.*").Where(s => s.EndsWith(".jpeg") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".png")));
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
                filelist.AddRange(Directory.GetFiles(folder, "*.*").Where(s => s.EndsWith(".jpeg") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".png")));
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
