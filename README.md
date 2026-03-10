# iOSDotNetTest

A prototype iOS "project template" for `dotnet test`.

See [the Android equivalent here](https://github.com/jonathanpeppers/AndroidDotNetTest).

## Usage

Run tests on the iOS simulator:

```bash
$ dotnet run
...
Test Suite 'DotNetTests' started at 2026-03-10 15:50:08.475.
Test Case '-[DotNetTestCase runMSTests]' started.
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

## XCTest Integration

Tests run inside Apple's [XCTest](https://developer.apple.com/documentation/xctest)
framework via `UIApplication.Main()` + `XCTestSuite`/`XCTestCase`. This gives the
app a proper UIKit run loop, preventing OS watchdog kills during long test runs.

A single `XCTestCase` wraps the entire MSTest execution — individual test results
are reported by MSTest itself (pass/fail/skip per test in the console and TRX).
The XCTest layer's job is to provide the UIKit run loop and signal to iOS that the
app is running tests, not to map each MSTest to its own `XCTestCase`.

### How Apple tests normally work

In an Xcode project, tests are a **separate target** that produces a `.xctest`
bundle (a dynamic library), not an executable. The Swift project in this repo
shows this — it has 3 targets:

| Target | Product Type | Output |
|--------|-------------|--------|
| `iOSDotNetTest` | `com.apple.product-type.application` | `.app` |
| `iOSDotNetTestTests` | `com.apple.product-type.bundle.unit-test` | `.xctest` |
| `iOSDotNetTestUITests` | `com.apple.product-type.bundle.ui-testing` | `.xctest` |

The normal flow is:

1. `xcodebuild test` builds the app and test bundle
2. Launches the app on the simulator
3. Injects the `.xctest` bundle into the running app
4. XCTest discovers all `XCTestCase` subclasses automatically
5. Runs them and reports results via `XCTestObservation`

We can't use this flow because `xcodebuild test` is a Mac-side tool that expects
an Xcode project — we don't have one. Instead, we drive XCTest manually from
inside the app.

### What the iOS workload would need for native `.xctest` support

For `dotnet test` to produce proper `.xctest` bundles and run via `xcodebuild test`,
the iOS workload (`dotnet/macios`) would need:

1. **Build as a dynamic library** — today the workload always produces a native
   executable. A `.xctest` bundle contains a dylib that XCTest loads at runtime.
2. **A host app** — `.xctest` bundles don't run standalone. The workload would
   need to produce a minimal host `.app` alongside the test bundle.
3. **XCTest entry point generation** — the dylib needs to export `XCTestCase`
   subclasses that XCTest discovers via the ObjC runtime. The workload would
   need to generate these from MSTest test methods.
4. **Xcode project/scheme generation** — `xcodebuild test` needs a scheme
   pointing to the host app + test bundle.
5. **New `OutputType`** — something like `<OutputType>XCTestBundle</OutputType>`
   that changes native linking from executable to dylib, generates the right
   `Info.plist`, and structures the output as `.xctest`.

### Current: P/Invoke-based XCTest interop

`Microsoft.iOS` does not ship bindings for XCTest, so we use raw ObjC runtime
P/Invokes to dynamically create an `XCTestCase` subclass and run it:

```csharp
// Current approach: 6 P/Invokes to libobjc.dylib
// 3 for dynamic class registration (no managed equivalent)
[DllImport("/usr/lib/libobjc.dylib")]
static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nint extraBytes);

[DllImport("/usr/lib/libobjc.dylib")]
static extern void objc_registerClassPair(IntPtr cls);

[DllImport("/usr/lib/libobjc.dylib")]
static extern bool class_addMethod(IntPtr cls, IntPtr sel, Delegate imp, string types);

// 3 for objc_msgSend (ObjCRuntime.Messaging has equivalents but is internal)
[DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
static extern IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);
```

### If `Microsoft.iOS` added XCTest bindings

With proper C# bindings for XCTest in `Microsoft.iOS`, the entire `XCTestBridge.cs`
could be replaced with:

```csharp
using XCTest;

var suite = XCTestSuite.Create("DotNetTests");
suite.AddTest(new DotNetTestCase("runMSTests"));
suite.RunTest();
```

Where `DotNetTestCase` would be:

```csharp
using XCTest;

[Register("DotNetTestCase")]
class DotNetTestCase : XCTestCase
{
    [Export("runMSTests")]
    public void RunMSTests()
    {
        MSTestRunner.RunAsync().GetAwaiter().GetResult();
    }
}
```

This would eliminate all 6 P/Invokes and the dynamic ObjC class registration.

### MSBuild target for linking XCTest.framework

XCTest is a "developer framework" not included in the standard SDK search paths.
The `_ResolveXCTestFramework` target in the `.csproj` resolves the Xcode platform
path and adds the necessary `-F` (framework search) and `-rpath` (runtime search)
linker flags. This target must inject into `_AllLinkerFlags` directly (not
`_CustomLinkFlags`) because `_AllLinkerFlags` is built in
`_ComputeLinkNativeExecutableInputs` which runs before `BeforeTargets` hooks on
`_LinkNativeExecutable`.
