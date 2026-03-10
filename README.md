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

## How It Works

The app uses `UIApplication.Main()` with an `AppDelegate` to establish a proper
UIKit run loop. This prevents iOS from watchdog-killing the app during long test
runs, which can happen when running tests directly from the entry point without
a UI run loop.

MSTest runs on a background thread from `AppDelegate.FinishedLaunching()`.
A custom `ResultConsumer` logs per-test pass/fail/skip results to the console and
calls `Environment.Exit()` once the TRX report is written, signaling completion.

`dotnet run` handles the full build → install → launch cycle via the iOS
workload's built-in mlaunch integration. The `GetResults` MSBuild target uses
`xcrun simctl` to locate the app's data container and copy TRX files locally.

### Workarounds

- **`MtouchInterpreter=all`**: Disables AOT compilation and uses the interpreter
  for all assemblies. MSTest platform DLLs have IL linker/AOT failures on iOS.
- **`GenerateTestingPlatformEntryPoint=false`**: MSTest SDK generates a `Main()`
  method that conflicts with our top-level statements entry point.
- **`--no-progress`**: MSTest's `TerminalTestReporter` calls `Console.BufferWidth`
  which throws `PlatformNotSupportedException` on iOS.
