using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests.HeadlessRunner
{
    public class TestResultsListener
    {
        public Task<bool> ListenAsync (int port, string saveTestResultsFilename, TimeSpan timeout)
        {
            var tcpListener = new TcpListener (IPAddress.Any, port);
            tcpListener.Start ();
            var listening = true;

            var cancelToken = new CancellationTokenSource (timeout);

            cancelToken.Token.Register (() => {
                if (listening) {
                    listening = false;
                    tcpListener.Stop ();
                }
            });

            return Task.Run (() => {
                try {
                    var tcpClient = tcpListener.AcceptTcpClient ();

                    using (var file = File.Open (saveTestResultsFilename, FileMode.Create))
                    using (var stream = tcpClient.GetStream ())
                        stream.CopyTo (file);

                    return true;
                } catch {
                    return false;
                } finally {
                    if (listening) {
                        listening = false;
                        tcpListener.Stop ();
                    }
                }
            });
        }
    }
}
