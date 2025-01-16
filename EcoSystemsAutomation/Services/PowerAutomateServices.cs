using System.Diagnostics;
using EcoSystemsAutomation.Contracts.ServiceContracts;
using Radzen;

namespace EcoSystemsAutomation.Services
{
    public class PowerAutomateServices : IPowerAutomateServices
    {
        private readonly IProcessInvokeService _processInvokeService;
        public PowerAutomateServices(IProcessInvokeService processInvokeService)
        {
            _processInvokeService = processInvokeService;
        }
        public void LoadPowerAutomate(string targetDir, string SCRIPTS_PATH)
        {
            try
            {
                if (Directory.GetFiles(targetDir).Contains(SCRIPTS_PATH))
                {
                    _processInvokeService.LoadProcess(SCRIPTS_PATH);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
