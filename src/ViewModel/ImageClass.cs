namespace Darknet.Dataset.Merger.ViewModel
{
    public class ImageClass
        : BaseViewModel
    {
        private string _name;
        private bool _active;
        private int _index;
        private bool _isPriorityInIndex;

        public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }
        public bool Active { get { return _active; } set { _active = value; OnPropertyChanged("Active"); } }
        public int Index { get { return _index; } set { _index = value; OnPropertyChanged("Index"); } }
        public bool IsPriorityInIndex { get { return _isPriorityInIndex; } set { _isPriorityInIndex = value; OnPropertyChanged("IsPriorityInIndex"); } }
    }
}
