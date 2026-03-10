using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace iOSDotNetTest;

class ResultConsumer : IDataConsumer
{
    public int Passed, Failed, Skipped;
    public string? TrxReportPath;

    public string Uid => nameof(ResultConsumer);
    public string DisplayName => nameof(ResultConsumer);
    public string Description => "";
    public string Version => "1.0";
    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Type[] DataTypesConsumed => [typeof(TestNodeUpdateMessage), typeof(SessionFileArtifact)];

    public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
    {
        if (value is SessionFileArtifact artifact)
        {
            TrxReportPath = artifact.FileInfo.FullName;

            Console.WriteLine($"Results: passed={Passed}, failed={Failed}, skipped={Skipped}");
            Console.WriteLine($"TRX report: {TrxReportPath}");
            // TestApplication.RunAsync() never returns on iOS, so exit from here
            Environment.Exit(Failed > 0 ? 1 : 0);
        }
        else if (value is TestNodeUpdateMessage { TestNode: var node })
        {
            var state = node.Properties.SingleOrDefault<TestNodeStateProperty>();
            string? outcome = state switch
            {
                PassedTestNodeStateProperty => "passed",
                FailedTestNodeStateProperty or ErrorTestNodeStateProperty
                    or TimeoutTestNodeStateProperty or CancelledTestNodeStateProperty => "failed",
                SkippedTestNodeStateProperty => "skipped",
                _ => null
            };
            if (outcome is null)
                return Task.CompletedTask;

            _ = outcome switch { "passed" => Passed++, "failed" => Failed++, _ => Skipped++ };

            var id = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
            var testName = id is not null ? $"{id.Namespace}.{id.TypeName}.{id.MethodName}" : node.DisplayName;
            Console.WriteLine($"[{outcome.ToUpperInvariant()}] {testName}");
        }
        return Task.CompletedTask;
    }
}
