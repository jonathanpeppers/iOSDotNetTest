namespace iOSDotNetTest;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        // Uncomment to slow down tests and see the UILabel update live:
        // Thread.Sleep(5000);
    }

    [TestMethod]
    public void TestMethod2()
    {
        // Thread.Sleep(5000);
        Assert.Fail("This test is expected to fail");
    }

    [TestMethod]
    public void TestMethod3()
    {
        // Thread.Sleep(5000);
        Assert.Inconclusive("This test is expected to be skipped");
    }
}
