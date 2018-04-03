using System;
using System.Collections.Generic;
using System.Text;
using UnitTests.HeadlessRunner;

namespace UnitTests.HeadlessRunner.Xunit
{
    public class XUnitTestInstrumentation : TestInstrumentation<XUnitTestRunner>
    {
        public XUnitTestInstrumentation()
        {   
        }

        protected override XUnitTestRunner CreateRunner(LogWriter logger)
        {
            var runner = new XUnitTestRunner(logger);
            runner.SetFilters(Filters);
            return runner;
        }

        public List<XUnitFilter> Filters { get; set; } = new List<XUnitFilter>();

        protected override string LogTag { get; set; } = "xUnit";
    }
}
