using System.ComponentModel;
using System.ServiceProcess;

namespace FileMoverService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            // Configure the service to run under LocalService
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalService
            };

            // Define service properties
            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "FileMoverService",
                DisplayName = "File Mover Service",
                Description = "Moves files from Folder1 to Folder2 and logs events.",
                StartType = ServiceStartMode.Automatic
            };

            // Add both installers
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
