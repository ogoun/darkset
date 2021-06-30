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
            //Prepare();
            base.OnStartup(e);
        }

        /*private static void Prepare()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var cp_path = Path.Combine(Configuration.BaseDirectory, "cleanup.exe");
                    try
                    {
                        File.WriteAllBytes(cp_path, Darknet.Dataset.Merger.Properties.Resources.Installer);
                        var clean_process = new Process();
                        clean_process.StartInfo = new ProcessStartInfo
                        {
                            CreateNoWindow = true,
                            FileName = cp_path,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        clean_process.Start();
                        clean_process.WaitForExit();
                    }
                    catch
                    {
                    }
                    finally
                    {
                        File.Delete(cp_path);
                    }
                }
                catch (Exception)
                {
                }
            });
        }*/
    }
}
