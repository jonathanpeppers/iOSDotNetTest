using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

namespace iOSDotNetTest;

static class MSTestRunner
{
    public static async Task<int> RunAsync()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var resultsPath = Path.Combine(documentsPath, "TestResults");

        var consumer = new ResultConsumer();
        var builder = await TestApplication.CreateBuilderAsync([
            "--results-directory", resultsPath,
            "--report-trx",
            "--no-progress"
        ]).ConfigureAwait(false);
        builder.AddMSTest(() => [typeof(Test1).Assembly]);
        builder.AddTrxReportProvider();
        builder.TestHost.AddDataConsumer(_ => consumer);

        using ITestApplication app = await builder.BuildAsync().ConfigureAwait(false);
        await app.RunAsync().ConfigureAwait(false);
        return consumer.Failed > 0 ? 1 : 0;
    }
}
