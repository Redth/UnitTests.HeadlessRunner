using UIKit;

namespace SampleTestApp.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            //if (args != null && args.Length > 0) {
            //    foreach (var arg in args)
            //        System.Diagnostics.Debug.WriteLine ("xUnit arg: {0}", arg);
            //}
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}