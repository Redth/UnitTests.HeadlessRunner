using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace UnitTests.HeadlessRunner
{
    public static class Tests
    {
        public static Task RunAsync (string listenerHost, int listenerPort, params Assembly[] testAssemblies)
        {
            return RunAsync (listenerHost, listenerPort, testAssemblies.Select (a => new TestAssemblyInfo (a, a.Location)).ToArray());
        }

        public static Task RunAsync(string listenerHost, int listenerPort, params TestAssemblyInfo[] testAssemblies)
        {
            /* Hack to preserve assembly during linking */
            var preserve = typeof(global::Xunit.Sdk.TestFailed);

            // Run the headless test runner for CI
            return System.Threading.Tasks.Task.Run(() =>
            {
                var xunitRunner = new Xunit.XUnitTestInstrumentation
                {
                    NetworkLogEnabled = true,
                    NetworkLogHost = listenerHost,
                    NetworkLogPort = listenerPort
                };

                xunitRunner.Run(testAssemblies);
            });
        }
    }
}
