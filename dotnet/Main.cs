using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;
using iOSDotNetTest;

var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
var resultsPath = Path.Combine(documentsPath, "TestResults");

try
{
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
