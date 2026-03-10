# iOSDotNetTest

A prototype iOS "project template" for `dotnet test`.

See [the Android equivalent here](https://github.com/jonathanpeppers/AndroidDotNetTest).

## Usage

Run tests on the iOS simulator:

```bash
$ dotnet run
...
[PASSED] iOSDotNetTest.Test1.TestMethod1
[FAILED] iOSDotNetTest.Test1.TestMethod2
[SKIPPED] iOSDotNetTest.Test1.TestMethod3
Results: passed=1, failed=1, skipped=1
TRX report: /path/to/TestResults/_Machine_2026-03-10_19_51_43.trx
```

Then collect the TRX results from the simulator:

```bash
$ dotnet build -t:GetResults
...
Test results copied to /path/to/TestResults/
```
