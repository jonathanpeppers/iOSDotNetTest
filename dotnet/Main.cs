using Foundation;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;
using ObjCRuntime;
using UIKit;
using iOSDotNetTest;

// UIApplication.Main() provides a proper UIKit run loop,
// preventing iOS watchdog kills during long test runs.
UIApplication.Main(args, null, typeof(AppDelegate));

[Register("AppDelegate")]
class AppDelegate : UIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        Task.Run(async () =>
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var resultsPath = Path.Combine(documentsPath, "TestResults");

                var builder = await TestApplication.CreateBuilderAsync([
                    "--results-directory", resultsPath,
                    "--report-trx",
                    "--no-progress"
                ]);
                builder.AddMSTest(() => [typeof(Test1).Assembly]);
                builder.AddTrxReportProvider();
                builder.TestHost.AddDataConsumer(_ => new ResultConsumer());

                using ITestApplication app = await builder.BuildAsync();
                await app.RunAsync();
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
