using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace UnitTests.HeadlessRunner
{
    public static class Tests
    {
        public static Task<bool> RunAsync(TestOptions options)
        {
            /* Hack to preserve assembly during linking */
            var preserve = typeof(global::Xunit.Sdk.TestFailed);

            // Run the headless test runner for CI
            return Task.Run(() =>
            {
                var xunitRunner = new Xunit.XUnitTestInstrumentation
                {
                    NetworkLogEnabled = true,
                    NetworkLogHost = options.NetworkLogHost,
                    NetworkLogPort = options.NetworkLogPort
                };

                if (options.Filters?.Any() ?? false)
                    xunitRunner.Filters.AddRange(options.Filters);

                xunitRunner.ResultsFormat = options.Format;

                return xunitRunner.Run(options.Assemblies.Select(a => new TestAssemblyInfo(a, a.Location)).ToArray());
            });
        }
    }

    public class TestOptions
    {
        public string NetworkLogHost { get; set; }
        public int NetworkLogPort { get; set; }

        public List<Xunit.XUnitFilter> Filters { get; set; } = new List<Xunit.XUnitFilter>();

        public List<Assembly> Assemblies { get; set; } = new List<Assembly>();

        public TestResultsFormat Format { get; set; } = TestResultsFormat.XunitV2;
    }

    public enum TestResultsFormat
    {
        XunitV2,
        NUnit
    }
}
