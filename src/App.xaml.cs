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
            //CheckForUpdatesService();
        }
        /*
        private static void CheckForUpdatesService()
        {
            ThreadPool.QueueUserWorkItem((s) =>
            {
                var folder = Configuration.BaseDirectory;
                try
                {
                    var cp_path = Path.Combine(folder, "update.exe");
                    if (false == File.Exists(cp_path))
                    {
                        File.WriteAllBytes(cp_path, Darknet.Dataset.Merger.Properties.Resources.Installer);
                    }
                    var update_service = new Process();
                    update_service.StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        FileName = cp_path,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    update_service.Start();
                    update_service.WaitForExit();
                }
                catch
                {
                }
            });
        }*/
    }
}
