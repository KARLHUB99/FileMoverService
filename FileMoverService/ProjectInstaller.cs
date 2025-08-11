using System.ComponentModel;
using System.ServiceProcess;

namespace FileMoverService
{
    // This attribute makes sure this installer runs when you install the service
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            // Set the account that the service will run under (LocalService in this case)
            var processInstaller = new ServiceProcessInstaller
            {
                Account = ServiceAccount.LocalService
            };

            // Define the service details like name, description, and how it starts
            var serviceInstaller = new ServiceInstaller
            {
                ServiceName = "FileMoverService",
                DisplayName = "File Mover Service",
                Description = "Moves files from Folder1 to Folder2 and logs events.",
                StartType = ServiceStartMode.Automatic // Starts automatically when Windows boots
            };

            // Add both installers so they run during installation
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
