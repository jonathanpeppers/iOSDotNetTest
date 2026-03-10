using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;

namespace iOSDotNetTest;

/// <summary>
/// Minimal XCTest interop via ObjC runtime P/Invokes.
/// ObjCRuntime.Messaging has equivalent objc_msgSend wrappers but is internal.
/// objc_allocateClassPair/objc_registerClassPair/class_addMethod have no managed equivalents.
/// </summary>
static class XCTestBridge
{
    static IntPtr _dotNetTestCaseClass;

    /// <summary>
    /// Dynamically registers a "DotNetTestCase" ObjC class as a subclass of XCTestCase,
    /// with a "runMSTests" instance method that runs MSTest.
    /// </summary>
    public static void RegisterDotNetTestCase()
    {
        var superclass = Class.GetHandle("XCTestCase");
        _dotNetTestCaseClass = objc_allocateClassPair(superclass, "DotNetTestCase", 0);

        var sel = Selector.GetHandle("runMSTests");
        class_addMethod(_dotNetTestCaseClass, sel, RunMSTestsImp, "v@:");

        objc_registerClassPair(_dotNetTestCaseClass);
    }

    delegate void ObjCMethodImp(IntPtr self, IntPtr selector);
    static readonly ObjCMethodImp RunMSTestsImp = RunMSTestsMethod;

    static void RunMSTestsMethod(IntPtr self, IntPtr selector)
    {
        Console.WriteLine("XCTestCase: Starting MSTest execution...");
        MSTestRunner.RunAsync().GetAwaiter().GetResult();
    }

    public static IntPtr CreateTestSuite(string name)
    {
        var cls = Class.GetHandle("XCTestSuite");
        var nameHandle = NSString.CreateNative(name);
        var suite = IntPtr_objc_msgSend_IntPtr(cls, Selector.GetHandle("testSuiteWithName:"), nameHandle);
        NSString.ReleaseNative(nameHandle);
        return suite;
    }

    public static IntPtr CreateTestCase()
    {
        return IntPtr_objc_msgSend_IntPtr(
            _dotNetTestCaseClass,
            Selector.GetHandle("testCaseWithSelector:"),
            Selector.GetHandle("runMSTests"));
    }

    public static void AddTest(IntPtr suite, IntPtr test)
    {
        void_objc_msgSend_IntPtr(suite, Selector.GetHandle("addTest:"), test);
    }

    public static void RunTest(IntPtr test)
    {
        void_objc_msgSend(test, Selector.GetHandle("runTest"));
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nint extraBytes);

    [DllImport("/usr/lib/libobjc.dylib")]
    static extern void objc_registerClassPair(IntPtr cls);

    [DllImport("/usr/lib/libobjc.dylib")]
    static extern bool class_addMethod(IntPtr cls, IntPtr sel, Delegate imp, string types);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);
}
