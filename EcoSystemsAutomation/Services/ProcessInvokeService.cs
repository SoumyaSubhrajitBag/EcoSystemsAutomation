using System.Diagnostics;
using EcoSystemsAutomation.Contracts.ServiceContracts;

namespace EcoSystemsAutomation.Services
{
    public class ProcessInvokeService : IProcessInvokeService
    {
        public void LoadProcess(string exePath)
        {
            try
            {
                // Simulate a double-click by invoking the file
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true, // Ensure the default application opens the file
                    WindowStyle = ProcessWindowStyle.Maximized // Attempt to maximize the window
                });
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
