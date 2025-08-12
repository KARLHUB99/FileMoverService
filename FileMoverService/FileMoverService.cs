using FileMoverServiceClib;
using System;
using System.Configuration;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace FileService
{
    public partial class FileMoverService : ServiceBase
    {
        // Keeps an eye on the source folder and moves files to the target folder
        private FolderMonitor _folderMonitor;

        // Used to log info, errors, and other messages
        private ILogger _logger;

        // Timer to regularly log that the service is still running (heartbeat)
        private Timer _heartbeatTimer;

        // Folder paths read from config
        private string SourceFolder;
        private string TargetFolder;

        // Names used for logging events in Windows Event Viewer
        private const string EventSourceName = "FileMoverServiceSource";
        private const string EventLogName = "FileMoverServiceLog";

        public FileMoverService()
        {
            InitializeComponent();

            // Set up the logger to write to both Windows Event Log and NLog
            _logger = new CompositeLogger(
                new EventLogger(EventSourceName, EventLogName),
                new NLogLogger()
            );

            // Read folder paths from App.config
            SourceFolder = ConfigurationManager.AppSettings["SourceFolder"];
            TargetFolder = ConfigurationManager.AppSettings["TargetFolder"];

            // Validate config values early
            if (string.IsNullOrWhiteSpace(SourceFolder) || string.IsNullOrWhiteSpace(TargetFolder))
            {
                throw new ConfigurationErrorsException("SourceFolder and TargetFolder must be set in App.config.");
            }
        }

        // Called when the service starts
        protected override void OnStart(string[] args)
        {
            _logger.Info("Starting FileMoverService...");

            try
            {
                // Check if the source and target folders exist before proceeding
                ValidateDirectories(SourceFolder, TargetFolder);

                // Create an object to handle moving files
                var mover = new FileMover(_logger);

                // Start watching the source folder for new files to move
                _folderMonitor = new FolderMonitor(SourceFolder, TargetFolder, mover, _logger);
                _folderMonitor.Start();

                _logger.Info("FileMoverService started successfully.");

                // Set a timer to log a heartbeat message every 5 minutes
                _heartbeatTimer = new Timer(LogHeartbeat, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
            catch (UnauthorizedAccessException uae)
            {
                _logger.Error("Access denied to source or target folder during startup.", uae);
                Stop();
            }
            catch (DirectoryNotFoundException dirEx)
            {
                _logger.Error("One or more directories not found during startup.", dirEx);
                Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error during startup.", ex);
                Stop();
            }
        }

        // Called when the service stops
        protected override void OnStop()
        {
            _logger.Info("Stopping FileMoverService...");

            try
            {
                _heartbeatTimer?.Dispose();
                _folderMonitor?.Stop();
                _folderMonitor?.Dispose();

                _logger.Info("FileMoverService stopped successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error("Error occurred while stopping FileMoverService.", ex);
            }
        }

        // Makes sure both folders exist, with retries if needed
        private void ValidateDirectories(string source, string target, int retries = 5, int delayMs = 2000)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (Directory.Exists(source) && Directory.Exists(target))
                        return;
                }
                catch (UnauthorizedAccessException)
                {
                    throw;
                }
                Thread.Sleep(delayMs);
            }

            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Source folder not found: {source}");

            if (!Directory.Exists(target))
                throw new DirectoryNotFoundException($"Target folder not found: {target}");
        }

        // Logs a message regularly to show the service is still running
        private void LogHeartbeat(object state)
        {
            _logger.Info("Heartbeat: FileMoverService is running.");
        }
    }
}
