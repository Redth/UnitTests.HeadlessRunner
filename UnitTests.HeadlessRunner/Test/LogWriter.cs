using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests.HeadlessRunner
{
    public class LogWriter
    {
        public MinimumLogLevel MinimumLogLevel { get; set; } = MinimumLogLevel.Info;

        public void OnError(string tag, string message)
        {
            if (MinimumLogLevel < MinimumLogLevel.Error)
                return;
            log(tag, message);
        }

        public void OnWarning(string tag, string message)
        {
            if (MinimumLogLevel < MinimumLogLevel.Warning)
                return;
            log(tag, message);
        }

        public void OnDebug(string tag, string message)
        {
            if (MinimumLogLevel < MinimumLogLevel.Debug)
                return;
            log(tag, message);
        }

        public void OnDiagnostic(string tag, string message)
        {
            if (MinimumLogLevel < MinimumLogLevel.Verbose)
                return;
            log(tag, message);
        }

        public void OnInfo(string tag, string message)
        {
            if (MinimumLogLevel < MinimumLogLevel.Info)
                return;
            log(tag, message);
        }

        void log(string tag, string message)
        {
            System.Console.WriteLine($"{tag}: {message}");
        }
    }
}
