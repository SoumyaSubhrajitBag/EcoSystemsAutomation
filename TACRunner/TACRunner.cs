using ASW.TestLab.Functional.Enums;
using ASW.TestLab.Functional.Sdk.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TACRunner
{
    [TestClass]
    public class TACRunner
    {
        [TestMethod]
        public void AutomationPass1(string scriptPath,string resultPath)
        {
            CommandLineOptions options = new CommandLineOptions
            {
                Browser = BrowserType.GoogleChrome,
                Filename = scriptPath,
                ResultsFilename = resultPath,
            };
            try
            {

                TestRunner runner = new TestRunner(options);
                runner.Run();
            }
            catch (Exception ex) { 
            
            }

        }
    }
}
