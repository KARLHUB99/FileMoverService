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

        // Store recently processed files and timestamps
        private readonly ConcurrentDictionary<string, DateTime> recentlyProcessed = new ConcurrentDictionary<string, DateTime>();


        private readonly TimeSpan debounceTime = TimeSpan.FromSeconds(5);

        // Semaphore to limit concurrency to 1 at a time
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public FolderMonitor(string sourceFolder, string targetFolder, FileMover mover, ILogger logger)
        {
            this.sourceFolder = sourceFolder;
            this.targetFolder = targetFolder;
            this.fileMover = mover;
            this.logger = logger;

            watcher = new FileSystemWatcher(sourceFolder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = false
            };

            watcher.Created += async (s, e) => await OnFileEventAsync(e.FullPath);
            watcher.Renamed += async (s, e) => await OnFileEventAsync(e.FullPath);
        }

        public void Start()
        {
            Directory.CreateDirectory(sourceFolder);
            Directory.CreateDirectory(targetFolder);
            recentlyProcessed.Clear();

            watcher.EnableRaisingEvents = true;
            logger.Info($"Monitoring started on '{sourceFolder}'.");
        }

        public void Stop()
        {
            watcher.EnableRaisingEvents = false;
            recentlyProcessed.Clear();

            logger.Info($"Monitoring stopped on '{sourceFolder}'.");
        }

        private async Task OnFileEventAsync(string filePath)
        {
            // Debounce: skip if processed recently
            if (recentlyProcessed.TryGetValue(filePath, out DateTime lastProcessed))
            {
                if (DateTime.Now - lastProcessed < debounceTime)
                {
                    logger.Info($"Skipping duplicate event for '{filePath}'.");
                    return;
                }
            }

            recentlyProcessed[filePath] = DateTime.Now;

            await semaphore.WaitAsync();
            try
            {
                logger.Info($"Detected new file: {filePath}");
                await Task.Run(() => fileMover.MoveFile(filePath, targetFolder));
            }
            catch (Exception ex)
            {
                logger.Error($"Error moving file '{filePath}'", ex);
            }
            finally
            {
                CleanupOldEntries();
                semaphore.Release();
            }
        }

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

        public void Dispose()
        {
            watcher.Dispose();
            semaphore.Dispose();
        }
    }
}
