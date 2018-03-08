using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests.HeadlessRunner
{
    internal static partial class Extensions
    {
        public static string YesNo(this bool b)
        {
            return b ? "yes" : "no";
        }
    }
}
