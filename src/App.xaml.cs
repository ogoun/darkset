using System.Windows;
using ZeroLevel;

namespace Darknet.Dataset.Merger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.AddTextFileLogger(new ZeroLevel.Logging.TextFileLoggerOptions().SetFolderPath("long"), ZeroLevel.Logging.LogLevel.FullStandart);
            base.OnStartup(e);
        }
    }
}
