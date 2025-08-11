using System.ServiceProcess;

namespace FileService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase.Run(new ServiceBase[] { new FileMoverService() });
        }
    }
}
