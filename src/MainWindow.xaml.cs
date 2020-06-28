using System.Windows;

namespace Darknet.Dataset.Merger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new MergerContext(this.Dispatcher);
        }

        private void ListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0)
            {
                var ds = e.AddedItems[0] as Dataset;
                if (ds != null)
                {
                    (this.DataContext as MergerContext)?.SelectDataset(ds);
                }
            }
        }
    }
}
