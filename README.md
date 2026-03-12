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

### Why not XCTest?

Apple's native test infrastructure uses `.xctest` bundles — dynamic libraries
that `xcodebuild test` injects into a host app at runtime. We explored this
approach on the [`XCTestFramework`](https://github.com/jonathanpeppers/iOSDotNetTest/tree/XCTestFramework)
branch, but running tests from `AppDelegate.FinishedLaunching()` is the better
path for now because:

1. **New project type** — the iOS workload would need to produce `.xctest` bundles
   (dylibs) instead of app executables, requiring a new `OutputType` and changes
   to the native linking pipeline in `dotnet/macios`.
2. **Developer framework support** — `Microsoft.iOS` doesn't ship bindings for
   XCTest. It's a "developer only" framework not included in standard SDK search
   paths, requiring custom linker flags (`-F`, `-rpath`) and either new bindings
   or raw P/Invokes.
3. **Two-project requirement** — in Xcode, tests require both a host `.app` and a
   `.xctest` bundle as separate targets. This doesn't map to other .NET platforms
   (Android, Windows) where a single test project is sufficient.
4. **Unclear benefit** — for running MSTest on a simulator, the main gain from
   native `.xctest` support would be the ability to run tests inside app
   extensions, bundles, or plugins. For the common case, the current approach
   works just as well.

### Workarounds

- **`MtouchInterpreter=all`**: Disables AOT compilation and uses the interpreter
  for all assemblies. MSTest platform DLLs have IL linker/AOT failures on iOS.
- **`GenerateTestingPlatformEntryPoint=false`**: MSTest SDK generates a `Main()`
  method that conflicts with our top-level statements entry point.
- **`--no-progress`**: MSTest's `TerminalTestReporter` calls `Console.BufferWidth`
  which throws `PlatformNotSupportedException` on iOS.
