using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// Base class to tests a class that is using the reactive framework.
/// </summary>
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

	/// <summary>
	/// A global cancellation token which will be cancelled on tear down.
	/// </summary>
	public CancellationToken CT => Context?.SourceContext.Token ?? TestContext?.CancellationTokenSource.Token ?? CancellationToken.None;

	/// <summary>
	/// Gets the context for the current feed test.
	/// </summary>
	protected FeedTestContext Context { get; private set; } = default!;

	/// <summary>
	/// Initialize the test context.
	/// </summary>
	[TestInitialize]
	public virtual void Initialize()
		=> Context = TestInitialize(TestContext);

	/// <summary>
	/// Tear down the test context.
	/// </summary>
	[TestCleanup]
	public virtual void Cleanup()
		=> TestCleanup(Context);
}
