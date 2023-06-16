namespace TestHarness.UITest.TestFramework;

public partial class RetryTests
{
	static int When_Retry_On_Unhandled_Exception_Count = 0;

	[Test]
	[AutoRetry]
	public void When_Retry_On_Unhandled_Exception()
	{
		Console.WriteLine($"When_Retry_On_Unhandled_Exception {++When_Retry_On_Unhandled_Exception_Count}");

		if (When_Retry_On_Unhandled_Exception_Count < 3)
		{
			throw new NotImplementedException();
		}
	}

}

public partial class RetrySetup
{
	static int Setup_Count = 0;

	[SetUp]
	public void Setup()
	{
		Console.WriteLine($"Setup: {++Setup_Count}");

		if (Setup_Count < 3)
		{
			throw new NotImplementedException();
		}
	}

	[Test]
	[AutoRetry]
	public void When_Success()
	{
		Console.WriteLine($"When_Success: {++Setup_Count}");
	}
}

public partial class RetryTearDown
{
	static int TearDown_Count = 0;

	[TearDown]
	public void TearDown()
	{
		Console.WriteLine($"TearDown: {++TearDown_Count}");

		if (TearDown_Count < 3)
		{
			throw new NotImplementedException();
		}
	}

	[Test]
	[AutoRetry]
	public void When_Success()
	{
		Console.WriteLine($"When_Success: {++TearDown_Count}");
	}
}
