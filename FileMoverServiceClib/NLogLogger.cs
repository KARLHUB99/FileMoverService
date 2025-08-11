using System;
using NLog;

namespace FileMoverServiceClib
{
    public class NLogLogger : ILogger
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public void Info(string message) => logger.Info(message);

        public void Error(string message, Exception ex)
        {
            logger.Error(ex, message);
        }
    }
}
