using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace UnitTests.HeadlessRunner
{
    public class TestAssemblyInfo
    {
        public Assembly Assembly { get; }
        public string FullPath { get; }

        public TestAssemblyInfo(Assembly assembly, string fullPath)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            FullPath = fullPath ?? String.Empty;
        }
    }
}
