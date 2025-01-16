namespace EcoSystemsAutomation.Contracts.ServiceContracts
{
    public interface IProcessInvokeService
    {
        void LoadProcess(string exePath);
        //Task RunAllScripts();
        //Task RunSingleScript(string scriptPath);
        //Task StopScript();
    }
}
