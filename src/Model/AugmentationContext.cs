using System.Collections.Generic;
using System.IO;

namespace Darknet.Dataset.Merger.Model
{
    public class AugmentationContext
    {
        private int _counter;
        private readonly string _augmetationFolder;
        private readonly string _objFolder;
        private readonly List<string> _files;

        public AugmentationContext(int counter, string objFolder, List<string> files)
        {
            _counter = counter;
            _augmetationFolder = Path.Combine(objFolder, "aug");
            _objFolder = objFolder;
            _files = files;
        }

        public bool WithoutClass { get; set; }
        public int Counter { get { return _counter; } }
        public IReadOnlyDictionary<string, int> Classes { get; set; }

        public (string, string) GetNextObjFileNames()
        {
            var imgFile = Path.Combine(_objFolder, _counter.ToString("D6") + ".jpg");
            var lblFile = Path.Combine(_objFolder, _counter.ToString("D6") + ".txt");
            _files.Add(imgFile);
            _counter++;
            return (imgFile, lblFile);
        }

        public (string, string) GetNextAugFileNames()
        {
            var imgFile = Path.Combine(_augmetationFolder, _counter.ToString("D6") + ".jpg");
            var lblFile = Path.Combine(_augmetationFolder, _counter.ToString("D6") + ".txt");
            _files.Add(imgFile);
            _counter++;
            return (imgFile, lblFile);
        }
    }
}
