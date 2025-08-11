using System;
using System.IO;

namespace FileMoverServiceClib
{
    public class RollingFileLogger : ILogger
    {
        private readonly string logDirectory;

        public RollingFileLogger(string logDirectory = null)
        {
            this.logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            if (!Directory.Exists(this.logDirectory))
                Directory.CreateDirectory(this.logDirectory);
        }

        private string GetLogFilePath()
        {
            string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            return Path.Combine(logDirectory, fileName);
        }

        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public void Error(string message, Exception ex)
        {
            string fullMessage = $"{message}\nException: {ex}";
            WriteLog("ERROR", fullMessage);
        }

        private void WriteLog(string level, string message)
        {
            string logFilePath = GetLogFilePath();
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
