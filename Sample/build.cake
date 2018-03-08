#addin "Cake.AppleSimulator"

var TARGET = Argument("target", "Default");

var APPLE_SIM_NAME = "iPhone X";
var APPLE_SIM_RUNTIME = "iOS 11.2";
var IOS_PROJ = "./SampleTestApp.iOS/SampleTestApp.iOS.csproj";
var IOS_BUNDLE_ID = "com.companyname.SampleTestApp.iOS";
var IOS_IPA_PATH = "./SampleTestApp.iOS/bin/iPhoneSimulator/Release/SampleTestApp.iOS.app";
var IOS_TEST_RESULTS_PATH = "./nunit-ios.xml";
var TCP_LISTEN_PORT = 10578;

Func<int, FilePath, Task> DownloadTcpTextAsync = (int port, FilePath filename) =>
    System.Threading.Tasks.Task.Run (() => {
        var tcpListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
        tcpListener.Start();

        var tcpClient = tcpListener.AcceptTcpClient();
        var fileName = MakeAbsolute (filename).FullPath;

        using (var file = System.IO.File.Open(fileName, System.IO.FileMode.Create))
        using (var stream = tcpClient.GetStream())
            stream.CopyTo(file);
    });


Task ("build-ios")
    .Does (() =>
{
    MSBuild (IOS_PROJ, c => {
        c.Configuration = "Release";
        c.Properties["Platform"] = new List<string> { "iPhoneSimulator" };
        c.Properties["BuildIpa"] = new List<string> { "true" };
    });
});

Task ("ios-simulator")
    .Does (() =>
{
    var sim = ListAppleSimulators ()
        .First (s => (s.Availability.Contains("available") || s.Availability.Contains("booted"))
                && s.Name == APPLE_SIM_NAME && s.Runtime == APPLE_SIM_RUNTIME);

    Information("Booting: {0}", sim.Name);
    if (!sim.State.ToLower().Contains ("booted"))
        BootAppleSimulator (sim.UDID);

    var booted = false;
    for (int i = 0; i < 100; i++) {
        if (ListAppleSimulators().Any (s => s.UDID == sim.UDID && s.State.ToLower().Contains("booted"))) {
            booted = true;
            break;
        }
    }

    var ipaPath = new FilePath(IOS_IPA_PATH);
    InstalliOSApplication(sim.UDID, MakeAbsolute(ipaPath).FullPath);

    var tcpListenerTask = DownloadTcpTextAsync (TCP_LISTEN_PORT, IOS_TEST_RESULTS_PATH);

    LaunchiOSApplication(sim.UDID, IOS_BUNDLE_ID);

    tcpListenerTask.Wait ();

    ShutdownAllAppleSimulators ();
});

RunTarget(TARGET);