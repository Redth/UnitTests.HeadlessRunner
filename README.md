# UnitTests.HeadlessRunner

There are several good options for running xUnit / NUnit tests on devices (iOS/Android), but they all work a little differently and they reside in different silos.  I could not find a great platform independent solution which would allow me to retrieve device test results from off of the device/emulator in a known format.

The Xamarin.Android team has [done some great work in building a test runner](https://github.com/xamarin/xamarin-android/tree/master/tests) that is almost platform independent.  I've borrowed the code, made it into a .netstandard 2.0 library that is easily consumable within any .NET app.

## Platform agnostic test result retrieval

My goal was to be able to support Android, iOS *and* UWP with this project.  There are different ways between platforms which one could use to retrieve test results from a device.  On Android, we could `adb pull` the test results file.  On iOS, if we use a simulator, we could just find the simulator's location on disk and copy over the test result file (but of course runnign on an actual device is another story).

Instead of maintaining these different methods (and figuring out how to do this on UWP at all), I decided to leverage one universal communication mechanism: *sockets*.

The idea is that you somehow (either hard code, inject during compilation or otherwise) tell the test runner application a *host address* and *host port* to connect to via TCP socket to send all the test result data to.  This means whatever invokes the actual test runner application needs to be listening on the socket for data.

When the data transmission completes, we know the tests are finished, and the test runner application can be terminated.

## Test Runner

To discover and run tests within any of your applications, at some point you will want to add the following code:

```csharp
UnitTests.HeadlessRunner.Tests.RunAsync (
    "192.168.1.100",
    10578,
    typeof(MyTestClass).Assembly);
```

## Test Result Listener

Really you can use any simple tcp socket listening to accept the raw ASCII endcoded xml file data, however I've also added a helper method to the library as well:

```csharp
var listenTask = UnitTests.HeadlessRunner.Tests.ListenAsync (
    port: 10578,
    saveTestResultsFilename: "nunit-results.xml",
    TimeSpan.FromSeconds(60));

// Start your test runner app now

listenTask.Wait ();

// Terminate your test runner app
```


## Limitations
 - xUnit only support (NUnit may be added later)
 - No choice in test result format - NUnit XML is the default