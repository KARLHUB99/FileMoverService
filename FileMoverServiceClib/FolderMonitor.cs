using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileMoverServiceClib
{
    public class FolderMonitor : IDisposable
    {
        private readonly FileSystemWatcher watcher;
        private readonly FileMover fileMover;
        private readonly ILogger logger;
        private readonly string sourceFolder;
        private readonly string targetFolder;

        // Keeps track of files recently processed to avoid handling duplicates too quickly
        private readonly ConcurrentDictionary<string, DateTime> recentlyProcessed = new ConcurrentDictionary<string, DateTime>();

        // Minimum time to wait before processing the same file again (debounce)
        private readonly TimeSpan debounceTime = TimeSpan.FromSeconds(5);

        // Semaphore to ensure only one file is processed at a time
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public FolderMonitor(string sourceFolder, string targetFolder, FileMover mover, ILogger logger)
        {
            this.sourceFolder = sourceFolder;
            this.targetFolder = targetFolder;
            this.fileMover = mover;
            this.logger = logger;

            // Set up the watcher to monitor file creations and renames in the source folder
            watcher = new FileSystemWatcher(sourceFolder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = false
            };

            // Attach async handlers for Created and Renamed events
            watcher.Created += async (s, e) => await OnFileEventAsync(e.FullPath);
            watcher.Renamed += async (s, e) => await OnFileEventAsync(e.FullPath);
        }

        // Starts monitoring the source folder and ensures both folders exist
        public void Start()
        {
            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(targetFolder);
            recentlyProcessed.Clear();

            watcher.EnableRaisingEvents = true;
            logger.Info($"Monitoring started on '{sourceFolder}'.");
        }

        // Stops monitoring and clears recent processed list
        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            recentlyProcessed.Clear();

            logger.Info($"Monitoring stopped on '{sourceFolder}'.");
        }

        // Called whenever a file is created or renamed in the source folder
        private async Task OnFileEventAsync(string filePath)
        {
            // If this file was processed recently, skip to avoid duplicate processing
            if (recentlyProcessed.TryGetValue(filePath, out DateTime lastProcessed))
            {
                if (DateTime.Now - lastProcessed < debounceTime)
                {
                    logger.Info($"Skipping duplicate event for '{filePath}'.");
                    return;
                }
            }

            // Mark this file as recently processed
            recentlyProcessed[filePath] = DateTime.Now;

            // Ensure only one file moves at a time using semaphore
            await semaphore.WaitAsync();
            try
            {
                logger.Info($"Detected new file: {filePath}");

                // Move the file using FileMover, run in a separate task to avoid blocking
                await Task.Run(() => fileMover.MoveFile(filePath, targetFolder));
            }
            catch (Exception ex)
            {
                logger.Error($"Error moving file '{filePath}'", ex);
            }
            finally
            {
                // Clean up old entries to prevent memory growth
                CleanupOldEntries();

                // Release the semaphore for next file
                semaphore.Release();
            }
        }

        // Removes entries from recentlyProcessed dictionary that are older than debounce time
        private void CleanupOldEntries()
        {
            var threshold = DateTime.Now - debounceTime;
            foreach (var kvp in recentlyProcessed)
            {
                if (kvp.Value < threshold)
                {
                    recentlyProcessed.TryRemove(kvp.Key, out _);
                }
            }
        }

        // Properly dispose of watcher and semaphore resources
        public void Dispose()
        {
            watcher.Dispose();
            semaphore.Dispose();
        }
    }
}
