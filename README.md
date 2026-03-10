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
