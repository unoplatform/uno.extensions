using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// The context of test which involves reactive framework.
/// </summary>
public class FeedTestContext : ISourceContextAware, IDisposable
{
	private static long _contextCount = 0;
	private readonly long _contextId = Interlocked.Increment(ref _contextCount);

	private readonly string _name;

	private SourceContext.CurrentSubscription _subscription;

	/// <summary>
	/// Creates a new context given the context of the test itself.
	/// </summary>
	/// <param name="testContext">The context of the test engine.</param>
	public FeedTestContext(TestContext testContext)
	{
		_name = testContext is null // On runtime test
			? $"runtime_tests.{_contextId:D4}"
			: $"{testContext.FullyQualifiedTestClassName}.{testContext.TestName}";

		SourceContext = SourceContext.GetOrCreate(this);
		_subscription = SourceContext.AsCurrent();

		testContext?.CancellationTokenSource.Token.Register(Dispose);
	}

	/// <summary>
	/// Creates a new context given a name used to identify it.
	/// </summary>
	/// <param name="name">The name used to identify this context. Do not provided: it will be provided by the compilator.</param>
	/// <exception cref="ArgumentNullException">If `null` is provided for the <paramref name="name"/>.</exception>
	public FeedTestContext([CallerMemberName] string? name = null)
	{
		_name = name ?? throw new ArgumentNullException("Context must be named.");
		SourceContext = SourceContext.GetOrCreate(this);
		_subscription = SourceContext.AsCurrent();
	}

	/// <summary>
	/// The <see cref="SourceContext"/> that has been created, and set as current, for the test.
	/// </summary>
	public SourceContext SourceContext { get; }

	/// <summary>
	/// Restores the <see cref="SourceContext"/> as <see cref="Core.SourceContext.Current"/>.
	/// </summary>
	public void RestoreCurrent()
	{
		ResignCurrent();
		_subscription = SourceContext.AsCurrent();
	}

	/// <summary>
	/// Removes the <see cref="SourceContext"/> from <see cref="Core.SourceContext.Current"/>.
	/// </summary>
	public void ResignCurrent()
		=> _subscription.Dispose();

	/// <inheritdoc />
	public void Dispose()
	{
		_subscription.Dispose();
		SourceContext.DisposeAsync().AsTask().Wait(1000);
	}

	/// <summary>
	/// Implicitly gets the <see cref="SourceContext"/> from a <see cref="FeedTestContext"/>.
	/// </summary>
	/// <param name="context"></param>
	public static implicit operator SourceContext(FeedTestContext context)
		=> context.SourceContext;

	/// <inheritdoc />
	public override string ToString()
		=> $"[FeedContext] {_name}";
}
