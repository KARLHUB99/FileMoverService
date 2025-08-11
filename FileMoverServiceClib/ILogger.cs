using System;

namespace FileMoverServiceClib
{
    public interface ILogger
    {
        void Info(string message);
        void Error(string message, Exception ex);
    }
}
