using Darknet.Dataset.Merger.ViewModel;
using System.Collections.Generic;

namespace Darknet.Dataset.Merger.Model
{
    public class MergeOptions
    {
        public bool WithoutClass = true;
        public string TargetFolder;
        public IEnumerable<Dataset> Datasets;
        public IEnumerable<ImageClass> Classes;
        public AugmentationOptions Augmentations;
    }
}
