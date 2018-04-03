using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace UnitTests.HeadlessRunner
{
    public abstract class TestInstrumentation<TRunner> where TRunner : TestRunner
    {
        const string ResultExecutedTests = "run";
        const string ResultPassedTests = "passed";
        const string ResultSkippedTests = "skipped";
        const string ResultFailedTests = "failed";
        const string ResultInconclusiveTests = "inconclusive";
        const string ResultTotalTests = "total";
        const string ResultFilteredTests = "filtered";
        const string ResultResultsFilePath = "nunit2-results-path";
        const string ResultError = "error";

        protected abstract string LogTag { get; set; }
        protected string TestAssembliesGlobPattern { get; set; }
        protected IList<string> TestAssemblyDirectories { get; set; }
        protected bool GCAfterEachFixture { get; set; }
        protected LogWriter Logger { get; } = new LogWriter();

        public string TestResultsFile { get; private set; }

        public bool NetworkLogEnabled { get;set; }
        public string NetworkLogHost { get;set; }
        public int NetworkLogPort { get;set; }

        protected TestInstrumentation()
        { }

        string FindTestAssembly(string name)
        {
            if (TestAssemblyDirectories == null || TestAssemblyDirectories.Count == 0)
                return null;

            AssemblyName aname = null;
            try
            {
                aname = new AssemblyName(name);
            }
            catch (Exception ex)
            {
                Logger.OnWarning(LogTag, $"Failed to parse assembly name: {name}");
                Logger.OnWarning(LogTag, ex.ToString());
            }

            if (aname == null)
                return null;

            foreach (string dir in TestAssemblyDirectories)
            {
                if (String.IsNullOrEmpty(dir))
                    continue;

                string path = Path.Combine(dir, aname.Name + ".dll");
                if (!File.Exists(path))
                    continue;

                return path;
            }

            return null;
        }

        public bool Run()
        {
            return Run (new TestAssemblyInfo[] {});
        }

        public bool Run(params Assembly[] extraTestAssemblies)
        {
            var a = extraTestAssemblies.Select (t => new TestAssemblyInfo (t, null));

            return Run (a.ToArray());
        }

        public bool Run(params TestAssemblyInfo[] extraTestAssemblies)
        {
            var success = false;

            try
            {
                success = RunTests(extraTestAssemblies);
            }
            catch (Exception ex)
            {
                Logger.OnError(LogTag, $"Error: {ex}");
                success = false;
            }

            return success;
        }

        void LogPaddedInfo(string name, string value, int alignColumn)
        {
            int padding = alignColumn - (name.Length + 1);
            if (padding <= 0)
                padding = 0;

            Logger.OnInfo(LogTag, $"[{name}:{new String(' ', padding)}{value}]");
        }

        bool RunTests(params TestAssemblyInfo[] extraTestAssemblies)
        {
            IList<TestAssemblyInfo> assemblies = GetTestAssemblies() ?? new List<TestAssemblyInfo>();

            if (extraTestAssemblies != null && extraTestAssemblies.Any())
                foreach (var extraAsm in extraTestAssemblies)
                    assemblies.Add(extraAsm);

            if (assemblies == null || assemblies.Count == 0)
            {
                Logger.OnInfo(LogTag, "No test assemblies loaded");
                return false;
            }

            TRunner runner = CreateRunner(Logger);
            runner.LogTag = LogTag;
            ConfigureFilters(runner);

            Logger.OnInfo(LogTag, "Starting unit tests");
            runner.Run(assemblies);
            Logger.OnInfo(LogTag, "Unit tests completed");

            TestResultsFile = runner.WriteResultsToFile();

            try {
                if (NetworkLogEnabled)
                    LogToTcp(NetworkLogHost, NetworkLogPort, TestResultsFile);
            } catch (Exception ex) {
                Logger.OnInfo (LogTag, $"Failed to send results to TCP Listener: {NetworkLogHost}:{NetworkLogPort} => " + ex);
            }

            return runner.FailedTests == 0;
        }


        void LogToTcp (string host, int port, string fromFile)
        {
            using (var tcpClient = new TcpClient ()) {

                tcpClient.Connect (host, port);

                using (var fileStream = File.OpenRead (fromFile))
                using (var networkStream = tcpClient.GetStream ())
                using (var clientStreamWriter = new StreamWriter (networkStream)) {
                    fileStream.CopyTo (networkStream);
                }

                tcpClient.Close();
            }
        }

        protected abstract TRunner CreateRunner(LogWriter logger);

        protected virtual IList<TestAssemblyInfo> GetTestAssemblies()
        {
            var ret = new List<TestAssemblyInfo>();

            if (TestAssemblyDirectories != null && TestAssemblyDirectories.Count > 0)
            {
                foreach (string adir in TestAssemblyDirectories)
                    GetTestAssembliesFromDirectory(adir, TestAssembliesGlobPattern, ret);
            }

            return ret;
        }

        protected virtual void GetTestAssembliesFromDirectory(string directoryPath, string globPattern, IList<TestAssemblyInfo> assemblies)
        {
            if (String.IsNullOrEmpty(directoryPath))
                throw new ArgumentException("must not be null or empty", nameof(directoryPath));

            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            string pattern = String.IsNullOrEmpty(globPattern) ? "*.dll" : globPattern;
            foreach (string file in Directory.EnumerateFiles(directoryPath, pattern, SearchOption.AllDirectories))
            {
                Logger.OnInfo(LogTag, $"Adding test assembly: {file}");
                Assembly asm;
                Exception ex = null;
                try
                {
                    asm = LoadTestAssembly(file);
                }
                catch (Exception e)
                {
                    asm = null;
                    ex = e;
                }

                if (asm == null)
                {
                    if (ex == null)
                        continue;
                    throw new InvalidOperationException($"Unable to load test assembly: {file}", ex);
                }

                // We store full path since Assembly.Location is not reliable on Android - it may hold a relative
                // path or no path at all
                assemblies.Add(new TestAssemblyInfo(asm, file));
            }
        }

        protected virtual Assembly LoadTestAssembly(string filePath)
        {
            return Assembly.LoadFrom(filePath);
        }

        protected virtual void ConfigureFilters(TRunner runner)
        { }

        protected virtual void ExtractAssemblies(string targetDir, Stream zipStream)
        {
            TestAssemblyDirectories = new List<string> {
                targetDir
            };

            if (Directory.Exists(targetDir))
            {
                foreach (string fi in Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories))
                {
                    File.Delete(fi);
                }
            }
            else
                Directory.CreateDirectory(targetDir);

            Logger.OnInfo(LogTag, $"Extracting test assemblies to {targetDir}");
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                zip.ExtractToDirectory(targetDir);
            }

            Logger.OnInfo(LogTag, "Extracted assemblies:");
            foreach (string fi in Directory.EnumerateFiles(targetDir, "*.dll"))
            {
                Logger.OnInfo(LogTag, $"  {fi}");
            }
        }

        protected HashSet<string> LoadExcludedTests(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            HashSet<string> excludedTestNames = null;
            do
            {
                string line = reader.ReadLine()?.Trim();
                if (line == null)
                    return excludedTestNames;

                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (excludedTestNames == null)
                    excludedTestNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (excludedTestNames.Contains(line))
                    continue;

                excludedTestNames.Add(line);
            } while (true);
        }
    }
}
