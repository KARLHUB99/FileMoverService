using System;
using System.IO;

namespace FileMoverServiceClib
{
    // Logger that writes messages to a daily rolling log file
    public class RollingFileLogger : ILogger
    {
        private readonly string logDirectory;

        public RollingFileLogger(string logDirectory = null)
        {
            // Use the provided directory or default to a "Logs" folder in the app base directory
            this.logDirectory = logDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // Make sure the log directory exists
            if (!Directory.Exists(this.logDirectory))
                Directory.CreateDirectory(this.logDirectory);
        }

        // Builds the log file path using today's date in the filename
        private string GetLogFilePath()
        {
            string fileName = $"log_{DateTime.Now:yyyyMMdd}.txt";
            return Path.Combine(logDirectory, fileName);
        }

        // Logs an informational message
        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        // Logs an error message including exception details
        public void Error(string message, Exception ex)
        {
            string fullMessage = $"{message}\nException: {ex}";
            WriteLog("ERROR", fullMessage);
        }

        // Writes the log entry to the appropriate daily log file
        private void WriteLog(string level, string message)
        {
            string logFilePath = GetLogFilePath();
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

            // Append the message to the file with a newline
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }
}
