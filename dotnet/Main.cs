using Foundation;
using ObjCRuntime;
using UIKit;
using iOSDotNetTest;

UIApplication.Main(args, null, typeof(AppDelegate));

[Register("AppDelegate")]
class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        XCTestBridge.RegisterDotNetTestCase();

        // XCTest requires main thread for observer registration,
        // so dispatch via the main queue after the run loop starts
        NSRunLoop.Main.InvokeOnMainThread(() =>
        {
            try
            {
                var suite = XCTestBridge.CreateTestSuite("DotNetTests");
                var testCase = XCTestBridge.CreateTestCase();
                XCTestBridge.AddTest(suite, testCase);
                XCTestBridge.RunTest(suite);
                // UIApplication.Main() keeps the process alive, so exit explicitly
                Environment.Exit(XCTestBridge.ExitCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests: {ex}");
                Environment.Exit(1);
            }
        });
        return true;
    }
}
