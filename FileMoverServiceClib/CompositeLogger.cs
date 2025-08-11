using System;
using System.Collections.Generic;

namespace FileMoverServiceClib
{
    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers;

        public CompositeLogger(params ILogger[] loggers)
        {
            _loggers = new List<ILogger>(loggers);
        }

        public void Info(string message)
        {
            foreach (var logger in _loggers)
                logger.Info(message);
        }

        public void Error(string message, Exception ex)
        {
            foreach (var logger in _loggers)
                logger.Error(message, ex);
        }
    }
}
