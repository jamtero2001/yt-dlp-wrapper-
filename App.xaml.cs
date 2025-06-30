using System.Windows;
using System.IO;

namespace YtDlpWrapper
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize application services
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Create temp directories if they don't exist
            var tempDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YtDlpWrapper", "temp_videos");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            // Create downloads directory if it doesn't exist
            var downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YtDlpWrapper", "Downloads");
            if (!Directory.Exists(downloadsDir))
            {
                Directory.CreateDirectory(downloadsDir);
            }
        }
    }
} 