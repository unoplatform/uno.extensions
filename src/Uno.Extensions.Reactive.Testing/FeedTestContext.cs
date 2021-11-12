using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Testing;

public class FeedTestContext : ISourceContextAware, IDisposable
{
	private readonly TestContext _testCtx;
	private readonly SourceContext _sourceCtx;
			
	private SourceContext.CurrentSubscription _subscription;

	public FeedTestContext(TestContext testContext)
	{
		_testCtx = testContext;

		_sourceCtx = SourceContext.GetOrCreate(this);
		_subscription = _sourceCtx.AsCurrent();
	}

	public SourceContext SourceContext => _sourceCtx;

	public void RestoreCurrent()
	{
		ResignCurrent();
		_subscription = _sourceCtx.AsCurrent();
	}

	public void ResignCurrent()
		=> _subscription.Dispose();

	/// <inheritdoc />
	public void Dispose()
	{
		_subscription.Dispose();
		_sourceCtx.DisposeAsync().AsTask().Wait(1000);
	}

	public static implicit operator SourceContext(FeedTestContext context)
		=> context._sourceCtx;

	/// <inheritdoc />
	public override string ToString()
		=> $"[FeedContext] {_testCtx.FullyQualifiedTestClassName}.{_testCtx.TestName}"; //{(_testCtx.DataRow is {} dataRow ? $" ({dataRow})" : "")}";
}
