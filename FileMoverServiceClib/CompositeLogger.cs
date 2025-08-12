using System;
using System.Collections.Generic;

namespace FileMoverServiceClib
{
    public class CompositeLogger : ILogger
    {
        // Holds the list of actual loggers to send messages to
        private readonly List<ILogger> _loggers;

        // Accepts any number of ILogger instances and keeps them in a list
        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers = new List<ILogger>(loggers);
        }

        // Logs an informational message by sending it to all loggers
        public void Info(string message)
        {
            foreach (var logger in _loggers)
                logger.Info(message);
        }

        // Logs an error message with exception details to all loggers
        public void Error(string message, Exception ex)
        {
            foreach (var logger in _loggers)
                logger.Error(message, ex);
        }
    }
}
