using System;
using System.IO;
using System.Threading;

namespace FileMoverServiceClib
{
    public class FileMover
    {
        private readonly ILogger logger;

        public FileMover(ILogger logger)
        {
            // Make sure a valid logger is provided
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Waits for the file to be free (not locked) so it can be safely accessed
        // Tries multiple times, with a delay between attempts
        public bool WaitForFileReady(string filePath, int retries = 10, int delayMs = 500)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    // Try to open the file with exclusive access
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        // If file size is greater than zero, consider it ready
                        if (stream.Length > 0)
                        {
                            logger.Info($"File '{filePath}' is ready for processing.");
                            return true;  // file is ready
                        }
                    }
                }
                catch (IOException)
                {
                    // If file is locked, log and wait before retrying
                    logger.Info($"File '{filePath}' not ready yet (attempt {i + 1}/{retries}). Retrying in {delayMs} ms...");
                    Thread.Sleep(delayMs);
                }
            }

            // After retries, still locked - log error and return false
            logger.Error($"File '{filePath}' is still locked after {retries} retries.", null);
            return false;  // file not ready
        }

        // Moves the file from source path to the target directory
        public void MoveFile(string sourcePath, string targetDir)
        {
            // Wait until the file is ready before moving
            if (!WaitForFileReady(sourcePath))
            {
                logger.Info($"Skipping move because file '{sourcePath}' was not ready.");
                return;  // skip move if file is locked or inaccessible
            }

            // Double-check that the file still exists before moving
            if (!File.Exists(sourcePath))
            {
                logger.Info($"File '{sourcePath}' no longer exists. Skipping move.");
                return;  // file was deleted or moved by someone else
            }

            // Prepare destination file path
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(targetDir, fileName);

            // Move the file and log success
            File.Move(sourcePath, destPath);
            logger.Info($"Successfully moved '{fileName}' from '{sourcePath}' to '{destPath}'.");
        }
    }
}
