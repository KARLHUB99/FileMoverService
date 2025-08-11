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
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool WaitForFileReady(string filePath, int retries = 10, int delayMs = 500)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        if (stream.Length > 0)
                        {
                            logger.Info($"File '{filePath}' is ready for processing.");
                            return true;  // file ready
                        }
                    }
                }
                catch (IOException)
                {
                    logger.Info($"File '{filePath}' not ready yet (attempt {i + 1}/{retries}). Retrying in {delayMs} ms...");
                    Thread.Sleep(delayMs);
                }
            }

            logger.Error($"File '{filePath}' is still locked after {retries} retries.", null);
            return false;  // file not ready
        }

        public void MoveFile(string sourcePath, string targetDir)
        {
            // Validation code here...

            if (!WaitForFileReady(sourcePath))
            {
                logger.Info($"Skipping move because file '{sourcePath}' was not ready.");
                return;  // skip move
            }

            // Check existence again
            if (!File.Exists(sourcePath))
            {
                logger.Info($"File '{sourcePath}' no longer exists. Skipping move.");
                return;
            }

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(targetDir, fileName);

            File.Move(sourcePath, destPath);
            logger.Info($"Successfully moved '{fileName}' from '{sourcePath}' to '{destPath}'.");
        }

    }
}
