// ProjectInstaller.cs - MSI installer setup
using System.ComponentModel;
using System.Configuration.Install;

namespace ModernGallery
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            // Add any necessary installation steps
            Committed += ProjectInstaller_Committed;
        }
        
        private void ProjectInstaller_Committed(object sender, InstallEventArgs e)
        {
            // Perform any post-installation tasks
            // e.g., create required directories, download models, etc.
            
            var modelsDirectory = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "ModernGallery",
                "Models");
            
            // Download models in a background process
            System.Diagnostics.Process.Start(System.IO.Path.Combine(Context.Parameters["assemblypath"], "ModernGallery.exe"), "--download-models");
        }
    }
}