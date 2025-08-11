using System;
using System.Diagnostics;
using System.IO;

namespace FileMoverServiceClib
{
    public class EventLogger : ILogger
    {
        private readonly EventLog eventLog;
        private readonly string logFilePath;

        private const string LogDirectoryName = "Logs";

        public EventLogger(string source, string logName)
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectoryName);
            Directory.CreateDirectory(logDir);

            logFilePath = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");

            try
            {
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, logName);
                }
            }
            catch (System.Security.SecurityException secEx)
            {
                WriteToFile("WARN", $"Could not create EventLog source. {secEx.Message}");
            }

            eventLog = new EventLog
            {
                Source = source,
                Log = logName
            };
        }

        public void Info(string message)
        {
            try
            {
                eventLog.WriteEntry(message, EventLogEntryType.Information);
            }
            catch { /* swallow event log write failures */ }

            WriteToFile("INFO", message);
        }

        public void Error(string message, Exception ex)
        {
            string fullMessage = ex == null
                ? message
                : $"{message}\nException: {ex}\nStack Trace: {ex.StackTrace}";

            try
            {
                eventLog.WriteEntry(fullMessage, EventLogEntryType.Error);
            }
            catch { /* swallow event log write failures */ }

            WriteToFile("ERROR", fullMessage);
        }

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
                    eventLog.WriteEntry($"Failed to write to log file. {ioEx.Message}", EventLogEntryType.Warning);
                }
                catch
                {
                    // Avoid recursive logging errors.
                }
            }
        }
    }
}
