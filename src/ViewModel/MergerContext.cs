﻿using Darknet.Dataset.Merger.Model;
using Darknet.Dataset.Merger.Services;
using Darknet.Dataset.Merger.ViewModel;
using Darknet.Dataset.Merger.Windows;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Darknet.Dataset.Merger
{
    public class MergerContext
        : BaseViewModel
    {
        #region Commands
        private ICommand _selectOutpouFolderCommand;
        private ICommand _appendDatasetCommand;
        private ICommand _mergeCommand;
        private ICommand _removeDatasetCommand;
        private ICommand _removeAllDatasetCommand;
        private ICommand _viewDatasetCommand;
        private ICommand _createDatasetCommand;

        public ICommand SelectOutpouFolderCommand => _selectOutpouFolderCommand;
        public ICommand AppendDatasetCommand => _appendDatasetCommand;
        public ICommand MergeCommand => _mergeCommand;
        public ICommand RemoveDatasetCommand => _removeDatasetCommand;
        public ICommand RemoveAllDatasetCommand => _removeAllDatasetCommand;
        public ICommand ViewDatasetCommand => _viewDatasetCommand;
        public ICommand CreateDatasetCommand => _createDatasetCommand;
        #endregion

        #region Augmentations
        public bool PrewittKernel
        {
            get { return _selectedDataset?.Augmentations?.PrewittKernel ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.PrewittKernel = value; OnPropertyChanged("PrewittKernel"); } }
        }
        public bool KayyaliKernel
        {
            get { return _selectedDataset?.Augmentations?.KayyaliKernel ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.KayyaliKernel = value; OnPropertyChanged("KayyaliKernel"); } }
        }
        public bool ScharrKernel
        {
            get { return _selectedDataset?.Augmentations?.ScharrKernel ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.ScharrKernel = value; OnPropertyChanged("ScharrKernel"); } }
        }
        public bool RobertsCrossKernel
        {
            get { return _selectedDataset?.Augmentations?.RobertsCrossKernel ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.RobertsCrossKernel = value; OnPropertyChanged("RobertsCrossKernel"); } }
        }
        public bool SobelKernel
        {
            get { return _selectedDataset?.Augmentations?.SobelKernel ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.SobelKernel = value; OnPropertyChanged("SobelKernel"); } }
        }
        
        public bool Grayscale
        {
            get { return _selectedDataset?.Augmentations?.Grayscale ?? false; }
            set { if (_selectedDataset != null) { _selectedDataset.Augmentations.Grayscale = value; OnPropertyChanged("Grayscale"); } }
        }

        public bool Sepia
        {
            get { return _selectedDataset?.Augmentations?.Sepia ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.Sepia = value; OnPropertyChanged("Sepia"); }
            }
        }

        public bool Blur
        {
            get { return _selectedDataset?.Augmentations?.Blur ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.Blur = value; OnPropertyChanged("Blur"); }
            }
        }

        public bool BBoxBlur
        {
            get { return _selectedDataset?.Augmentations?.BBoxBlur ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.BBoxBlur = value; OnPropertyChanged("BBoxBlur"); }
            }
        }

        public bool Charcoal
        {
            get { return _selectedDataset?.Augmentations?.Charcoal ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.Charcoal = value; OnPropertyChanged("Charcoal"); }
            }
        }

        public bool Cut
        {
            get { return _selectedDataset?.Augmentations?.Cut ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.Cut = value; OnPropertyChanged("Cut"); }
            }
        }

        public bool ResizeToInput
        {
            get { return _selectedDataset?.Augmentations?.ResizeToInput ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.ResizeToInput = value; OnPropertyChanged("ResizeToInput"); }
            }
        }

        public string InputWidth
        {
            get { return _selectedDataset?.Augmentations?.InputWidth.ToString(); }
            set
            {
                if (_selectedDataset != null)
                {
                    int test;
                    if (int.TryParse(value, out test))
                    {
                        _selectedDataset.Augmentations.InputWidth = test;
                    }
                    OnPropertyChanged("InputWidth");
                }
            }
        }

        public string InputHeight
        {
            get { return _selectedDataset?.Augmentations?.InputHeight.ToString(); }
            set
            {
                if (_selectedDataset != null)
                {
                    int test;
                    if (int.TryParse(value, out test))
                    {
                        _selectedDataset.Augmentations.InputHeight = test;
                    }
                    OnPropertyChanged("InputHeight");
                }
            }
        }

        public bool OverrideFragments
        {
            get { return _selectedDataset?.Augmentations?.CutOverlaps ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.CutOverlaps = value; OnPropertyChanged("OverrideFragments"); }
            }
        }

        public bool LinesNoise
        {
            get { return _selectedDataset?.Augmentations?.LinesNoise ?? false; }
            set
            {
                if (_selectedDataset != null) { _selectedDataset.Augmentations.LinesNoise = value; OnPropertyChanged("LinesNoise"); }
            }
        }

        public string CutWidth
        {
            get { return _selectedDataset?.Augmentations?.CutWidth.ToString(); }
            set
            {
                if (_selectedDataset != null)
                {
                    int test;
                    if (int.TryParse(value, out test))
                    {
                        _selectedDataset.Augmentations.CutWidth = test;
                    }
                    OnPropertyChanged("CutWidth");
                }
            }
        }

        public string CutHeight
        {
            get { return _selectedDataset?.Augmentations?.CutHeight.ToString(); }
            set
            {
                if (_selectedDataset != null)
                {
                    int test;
                    if (int.TryParse(value, out test))
                    {
                        _selectedDataset.Augmentations.CutHeight = test;
                    }
                    OnPropertyChanged("CutHeight");
                }
            }
        }
        #endregion

        #region Progress
        private int _progressMax;
        private int _progressCurrent;

        public int ProgressCurrent
        {
            get
            {
                return _progressCurrent;
            }
            set
            {
                _progressCurrent = value;
                OnPropertyChanged("ProgressCurrent");
            }
        }

        public int ProgressMax
        {
            get
            {
                return _progressMax;
            }
            set
            {
                _progressMax = value;
                OnPropertyChanged("ProgressMax");
            }
        }
        #endregion

        #region UI Fields
        private string _outputFolder;
        private string _info;
        public ObservableCollection<Dataset> Datasets { get; set; }
        public ObservableCollection<ImageClass> Classes { get; set; }

        public string OutputFolder
        {
            get
            {
                return _outputFolder;
            }
            set
            {
                _outputFolder = value;
                OnPropertyChanged("OutputFolder");
            }
        }

        public bool WithoutClass
        {
            get
            {
                return _selectedDataset?.Augmentations?.WithoutClass ?? false;
            }
            set
            {
                if (_selectedDataset != null)
                {
                    _selectedDataset.Augmentations.WithoutClass = value;
                    OnPropertyChanged("WithoutClass");
                }
            }
        }

        public string Info
        {
            get
            {
                return _info;
            }
            set
            {
                _info = value;
                OnPropertyChanged("Info");
            }
        }
        #endregion

        private bool _merge_process = false;
        private readonly Dispatcher _uiDispatcher;
        private Dataset _selectedDataset;

        public MergerContext(Dispatcher uiDispatcher)
        {
            _uiDispatcher = uiDispatcher;
            _selectOutpouFolderCommand = new RelayCommand(_ => !_merge_process, SelectOutputFolder);
            _appendDatasetCommand = new RelayCommand(_ => !_merge_process, AppendDataset);
            _mergeCommand = new RelayCommand(_ => !_merge_process, Merge);

            _removeDatasetCommand = new RelayCommand(_ => !_merge_process, RemoveDataset);
            _removeAllDatasetCommand = new RelayCommand(_ => !_merge_process, RemoveAllDatasets);
            _viewDatasetCommand = new RelayCommand(_ => !_merge_process, ViewDataset);

            _createDatasetCommand = new RelayCommand(_ => !_merge_process, CreateDataset);

            Datasets = new ObservableCollection<Dataset>();
            Classes = new ObservableCollection<ImageClass>();
        }

        public void SelectDataset(Dataset dataset)
        {
            _selectedDataset = dataset;

            // i know.. i know.. but i so lazy
            OnPropertyChanged("PrewittKernel");
            OnPropertyChanged("KayyaliKernel");
            OnPropertyChanged("ScharrKernel");
            OnPropertyChanged("RobertsCrossKernel");
            OnPropertyChanged("SobelKernel");

            OnPropertyChanged("Grayscale");
            OnPropertyChanged("Sepia");
            OnPropertyChanged("Blur");
            OnPropertyChanged("Charcoal");
            OnPropertyChanged("BBoxBlur");
            OnPropertyChanged("LinesNoise");

            OnPropertyChanged("Cut");
            OnPropertyChanged("OverrideFragments");
            OnPropertyChanged("CutWidth");
            OnPropertyChanged("CutHeight");

            OnPropertyChanged("ResizeToInput");
            OnPropertyChanged("InputWidth");
            OnPropertyChanged("InputHeight");

            OnPropertyChanged("WithoutClass");
        }

        public void SelectOutputFolder(object state)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            if ((bool)dialog.ShowDialog())
            {
                OutputFolder = dialog.SelectedPath;
            }
        }

        public void AppendDataset(object state)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            if ((bool)dialog.ShowDialog())
            {
                Dataset ds = null;
                try
                {
                    ds = new Dataset(dialog.SelectedPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                if (ds != null)
                {
                    Datasets.Add(ds);
                    RecalculateDatasetsInfo();
                }
            }
        }

        public void RemoveDataset(object state)
        {
            if (_selectedDataset != null)
            {
                Datasets.Remove(_selectedDataset);
            }
            RecalculateDatasetsInfo();
        }

        public void ViewDataset(object state)
        {
            if (_selectedDataset != null)
            {
                var w = new DatasetViewWindow(_selectedDataset);
                w.ShowDialog();
            }
        }

        public void CreateDataset(object state)
        {
            VistaFolderBrowserDialog dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Please select a folder.";
            dialog.UseDescriptionForTitle = true;
            if ((bool)dialog.ShowDialog())
            {
                var w = new DatasetMakeWindow(dialog.SelectedPath);
                if (true == w.ShowDialog())
                {
                    Dataset ds = null;
                    try
                    {
                        ds = new Dataset(dialog.SelectedPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                    if (ds != null)
                    {
                        Datasets.Add(ds);
                        RecalculateDatasetsInfo();
                    }
                }
            }
        }

        public void RemoveAllDatasets(object state)
        {
            Datasets.Clear();
            RecalculateDatasetsInfo();
        }

        private void RecalculateDatasetsInfo()
        {
            int imageCount = 0;
            var classes = new HashSet<string>();
            foreach (var ds in Datasets)
            {
                foreach (var c in ds.Classes) classes.Add(c);
                imageCount += ds.Images.Count;
            }
            Info = $"Images: {imageCount}\tClasses: {classes.Count}";

            var maxIndex = Classes.Count == 0 ? -1 : Classes.Max(c => c.Index);
            foreach (var c in classes)
            {
                if (!Classes.Any(cn => cn.Name.Equals(c, System.StringComparison.OrdinalIgnoreCase)))
                {
                    maxIndex++;
                    Classes.Add(new ImageClass
                    {
                        Active = true,
                        Index = maxIndex,
                        IsPriorityInIndex = true,
                        Name = c
                    });
                }
            }
            ProgressMax = imageCount;
        }

        public void Merge(object state)
        {
            if (Datasets?.Count == 0)
            {
                MessageBox.Show("No added datasets");
                return;
            }
            if (string.IsNullOrWhiteSpace(_outputFolder))
            {
                MessageBox.Show("No defined target folder");
                return;
            }
            _merge_process = true;
            ThreadPool.QueueUserWorkItem(s =>
            {
                DatasetMerger.Merge(new MergeOptions
                {
                    TargetFolder = _outputFolder,
                    Datasets = Datasets,
                    Classes = Classes
                }, p =>
                {
                    _uiDispatcher.Invoke(() =>
                    {
                        ProgressCurrent = p;
                    });
                });
                _merge_process = false;
            });
        }
    }
}
