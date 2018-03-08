using System;
using System.Collections.Generic;
using System.Text;
using UnitTests.HeadlessRunner;

namespace UnitTests.HeadlessRunner.Xunit
{
    internal class XUnitTestInstrumentation : TestInstrumentation<XUnitTestRunner>
    {
        public XUnitTestInstrumentation()
        {   
        }

        protected override XUnitTestRunner CreateRunner(LogWriter logger)
        {
            return new XUnitTestRunner(logger);
        }

        protected override string LogTag { get; set; } = "xUnit";
    }
}
