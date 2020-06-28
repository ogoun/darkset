using System.Collections.Generic;
using System.Windows;

namespace Darknet.Dataset.Merger.Windows
{
    /// <summary>
    /// Interaction logic for ClassSelectionWindow.xaml
    /// </summary>
    public partial class ClassSelectionWindow : Window
    {
        public string SelectedClass { get; private set; }

        public ClassSelectionWindow(List<string> classes)
        {
            InitializeComponent();
            this.DataContext = classes;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (lbClasses.SelectedItem != null)
            {
                SelectedClass = lbClasses.SelectedItem as string;
                this.DialogResult = true;
            }
            else
            {
                this.DialogResult = false;
            }
            Close();
        }
    }
}
