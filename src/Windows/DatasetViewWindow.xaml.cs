using Darknet.Dataset.Merger.Model;
using Darknet.Dataset.Merger.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace Darknet.Dataset.Merger.Windows
{
    /// <summary>
    /// Interaction logic for DatasetViewWindow.xaml
    /// </summary>
    public partial class DatasetViewWindow : Window
    {
        
        public DatasetViewWindow(Dataset dataset)
        {
            InitializeComponent();
            this.DataContext = new DatasetViewerContext(dataset);
        }

        private void TrainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbTest.SelectedItem = null;
            var image = lbTrain.SelectedItem as ImageInfo;
            if (image != null)
            {
                (this.DataContext as DatasetViewerContext).SetCurrentImage(image);
            }
        }

        private void TestListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lbTrain.SelectedItem = null;
            var image = lbTest.SelectedItem as ImageInfo;
            if (image != null)
            {
                (this.DataContext as DatasetViewerContext).SetCurrentImage(image);
            }
        }
    }
}
