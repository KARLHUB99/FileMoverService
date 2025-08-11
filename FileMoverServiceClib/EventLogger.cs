using System;
using System.Diagnostics;
using System.IO;

namespace FileMoverServiceClib
{
    // Logger that writes messages both to Windows Event Log and to daily log files
    public class EventLogger : ILogger
    {
        private readonly EventLog eventLog;
        private readonly string logFilePath;

        // Folder name where log files will be saved
        private const string LogDirectoryName = "Logs";

        public EventLogger(string source, string logName)
        {
            // Prepare the folder path for logs inside the app directory
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectoryName);
            Directory.CreateDirectory(logDir);

            // Set the log file path with current date as filename
            logFilePath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");

            try
            {
                // Check if the event source exists, create it if not
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }
            }
            catch (System.Security.SecurityException secEx)
            {
                // If we don't have permissions to create event source, log warning to file instead
                WriteToFile("WARN", $"Could not create EventLog source. {secEx.Message}");
            }

            // Initialize EventLog instance for writing logs
            eventLog = new EventLog
            {
                Source = source,
                Log = logName
            };
        }

        // Log informational message to event log and file
        public void Info(string message)
        {
            try
            {
                eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch
            {
                // Ignore failures writing to event log so service doesn't crash
            }

            WriteToFile("INFO", message);
        }

        // Log error message and exception details to event log and file
        public void Error(string message, Exception ex)
        {
            string fullMessage = ex == null
                ? message
                : $"{message}\nException: {ex}\nStack Trace: {ex.StackTrace}";

            try
            {
                eventLog.WriteEntry(fullMessage, EventLogEntryType.Error);
            }
            catch
            {
                // Ignore failures writing to event log
            }

            WriteToFile("ERROR", fullMessage);
        }

        // Append log messages to daily text file, creating it if needed
        private void WriteToFile(string level, string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (IOException ioEx)
            {
                try
                {
                    // If writing to file fails, try to log warning in event log
                    eventLog.WriteEntry($"Failed to write to log file. {ioEx.Message}", EventLogEntryType.Warning);
                }
                catch
                {
                    // Avoid recursive errors if even this fails
                }
            }
        }
    }
}
