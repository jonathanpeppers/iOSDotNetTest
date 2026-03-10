using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

namespace iOSDotNetTest;

static class MSTestRunner
{
    public static async Task RunAsync()
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
}
