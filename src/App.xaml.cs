using System.Diagnostics;
using System.IO;
using System.Threading;
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
            Log.AddTextFileLogger(new ZeroLevel.Logging.TextFileLoggerOptions().SetFolderPath("logs"), ZeroLevel.Logging.LogLevel.FullStandart);
            base.OnStartup(e);
        }
    }
}
