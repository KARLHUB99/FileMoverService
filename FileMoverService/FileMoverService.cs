using FileMoverServiceClib;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace FileService
{
    public partial class FileMoverService : ServiceBase
    {
        private FolderMonitor _folderMonitor;
        private ILogger _logger;
        private Timer _heartbeatTimer;

        private const string SourceFolder = @"C:\A Folder";
        private const string TargetFolder = @"C:\B Folder";
        private const string EventSourceName = "FileMoverServiceSource";
        private const string EventLogName = "FileMoverServiceLog";

        public FileMoverService()
        {
            InitializeComponent();

            // Future improvement: inject loggers via DI
            _logger = new CompositeLogger(
                new EventLogger(EventSourceName, EventLogName),
                new NLogLogger()
            );
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Attempting to start FileMoverService...");

            try
            {
                ValidateDirectories(SourceFolder, TargetFolder);

                var mover = new FileMover(_logger);
                _folderMonitor = new FolderMonitor(SourceFolder, TargetFolder, mover, _logger);
                _folderMonitor.Start();

                _logger.Info("FileMoverService started successfully.");

                // Start heartbeat log every 5 minutes
                _heartbeatTimer = new Timer(LogHeartbeat, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            }
            catch (UnauthorizedAccessException uae)
            {
                _logger.Error("Access denied to source or target directory during service start.", uae);
                Stop();
            }
            catch (DirectoryNotFoundException dirEx)
            {
                _logger.Error("Directory validation failed during service start.", dirEx);
                Stop();
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error occurred while starting service.", ex);
                Stop();
            }
        }

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
                    throw; // rethrow for higher-level handling
                }

                Thread.Sleep(delayMs);
            }

            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Source folder does not exist: {source}");

            if (!Directory.Exists(target))
                throw new DirectoryNotFoundException($"Target folder does not exist: {target}");
        }

        private void LogHeartbeat(object state)
        {
            _logger.Info("Service heartbeat: FileMoverService is running.");
        }
    }
}
