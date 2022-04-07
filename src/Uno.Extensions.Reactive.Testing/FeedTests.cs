using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Testing;

public class FeedTests
{
	/// <summary>
	/// Initialize a test environment without inheriting from this base class.
	/// </summary>
	/// <param name="ctx">The context of the test engine.</param>
	/// <returns>The context for feeds of the current test.</returns>
	public static FeedTestContext TestInitialize(TestContext ctx)
		=> new(ctx);

	/// <summary>
	/// Cleanup the test environment without inheriting from this base class.
	/// </summary>
	/// <param name="ctx">The context for feeds of the current test that was initialized in the <see cref="TestInitialize"/>.</param>
	public static void TestCleanup(FeedTestContext ctx)
		=> ctx.Dispose();

	/// <summary>
	/// The test context of the test engine.
	/// </summary>
	public TestContext TestContext { get; set; } = default!;

	public CancellationToken CT => TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	/// <summary>
	/// Gets the context for the current feed test.
	/// </summary>
	protected FeedTestContext Context { get; private set; } = default!;

	[TestInitialize]
	public virtual void Initialize()
		=> Context = TestInitialize(TestContext);

	[TestCleanup]
	public virtual void Cleanup()
		=> TestCleanup(Context);
}
