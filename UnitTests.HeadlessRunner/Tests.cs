using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace UnitTests.HeadlessRunner
{
    public static class Tests
    {
        public static Task<bool> RunAsync(string listenerHost, int listenerPort, params Assembly[] testAssemblies)
        {
            return RunAsync(listenerHost, listenerPort, null, testAssemblies.Select(a => new TestAssemblyInfo(a, a.Location)).ToArray());
        }

        public static Task<bool> RunAsync(string listenerHost, int listenerPort, params TestAssemblyInfo[] testAssemblies)
        {
            return RunAsync(listenerHost, listenerPort, null, testAssemblies);
        }

        public static Task<bool> RunAsync (string listenerHost, int listenerPort, List<Xunit.XUnitFilter> filters, params Assembly[] testAssemblies)
        {
            return RunAsync (listenerHost, listenerPort, filters, testAssemblies.Select (a => new TestAssemblyInfo (a, a.Location)).ToArray());
        }

        public static Task<bool> RunAsync(string listenerHost, int listenerPort, List<Xunit.XUnitFilter> filters, params TestAssemblyInfo[] testAssemblies)
        {
            /* Hack to preserve assembly during linking */
            var preserve = typeof(global::Xunit.Sdk.TestFailed);

            // Run the headless test runner for CI
            return Task.Run(() =>
            {
                var xunitRunner = new Xunit.XUnitTestInstrumentation
                {
                    NetworkLogEnabled = true,
                    NetworkLogHost = listenerHost,
                    NetworkLogPort = listenerPort
                };

                if (filters != null && filters.Any())
                    xunitRunner.Filters.AddRange(filters);

                return xunitRunner.Run(testAssemblies);
            });
        }
    }
}
