using System;
using System.IO;
using System.Windows;

namespace YtDlpWrapper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Constants
        private const string ErrorLogFileName = "error.log";
        #endregion
        
        #region Private Fields
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ErrorLogFileName);
        #endregion

        /// <summary>
        /// Raises the Startup event and sets up global exception handling.
        /// </summary>
        /// <param name="e">A StartupEventArgs that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set up global exception handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
        }

        /// <summary>
        /// Handles unhandled exceptions in the UI thread.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The exception event data.</param>
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show($"An unexpected error occurred: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        /// <summary>
        /// Handles unhandled exceptions in background threads.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The exception event data.</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
            }
        }

        /// <summary>
        /// Logs an exception to the error log file.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        private static void LogException(Exception ex)
        {
            if (ex == null) return;
            
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(LogFilePath, logMessage);
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }
    }
} 