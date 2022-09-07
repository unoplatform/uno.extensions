using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Testing;

public static class FeedRecorderExtensions
{
	public static FeedRecorder<IFeed<T>, T> Record<T>(
		this IFeed<T> feed,
		SourceContext? context = null,
		bool autoEnable = true,
		[CallerArgumentExpression("feed")] string? feedExpression = null,
		[CallerMemberName] string? memberName = null,
		[CallerLineNumber] int line = -1)
		=> new(_ => feed, context ?? SourceContext.Current, autoEnable, feedExpression ?? $"{memberName}@{line}");

	public static FeedRecorder<IListFeed<T>, IImmutableList<T>> Record<T>(
		this IListFeed<T> feed,
		SourceContext? context = null,
		bool autoEnable = true,
		[CallerArgumentExpression("feed")] string? feedExpression = null,
		[CallerMemberName] string? memberName = null,
		[CallerLineNumber] int line = -1)
		=> new(_ => feed, context ?? SourceContext.Current, autoEnable, feedExpression ?? $"{memberName}@{line}");

	public static FeedRecorder<IState<T>, T> Record<T>(
		this IState<T> state,
		SourceContext? context = null,
		bool autoEnable = true,
		[CallerArgumentExpression("state")] string? feedExpression = null,
		[CallerMemberName] string? memberName = null,
		[CallerLineNumber] int line = -1)
		=> new(_ => state, context ?? SourceContext.Current, autoEnable, feedExpression ?? $"{memberName}@{line}");

	public static FeedRecorder<IListState<T>, IImmutableList<T>> Record<T>(
		this IListState<T> state,
		SourceContext? context = null,
		bool autoEnable = true,
		[CallerArgumentExpression("state")] string? feedExpression = null,
		[CallerMemberName] string? memberName = null,
		[CallerLineNumber] int line = -1)
		=> new(_ => state, context ?? SourceContext.Current, autoEnable, feedExpression ?? $"{memberName}@{line}");

	public static ValueTask WaitForMessages<T>(this IFeedRecorder<T> recorder, int count)
		=> recorder.WaitForMessages(count, FeedRecorder.DefaultTimeout, SourceContext.Current.Token);

	public static ValueTask WaitForMessages<T>(this IFeedRecorder<T> recorder, int count, int timeout)
		=> recorder.WaitForMessages(count, timeout, SourceContext.Current.Token);

	public static ValueTask WaitForMessages<T>(this IFeedRecorder<T> recorder, int count, CancellationToken ct)
		=> recorder.WaitForMessages(count, FeedRecorder.DefaultTimeout, ct);

	public static ValueTask WaitForEnd<T>(this IFeedRecorder<T> recorder)
		=> recorder.WaitForEnd(FeedRecorder.DefaultTimeout, SourceContext.Current.Token);

	public static ValueTask WaitForEnd<T>(this IFeedRecorder<T> recorder, int timeout)
		=> recorder.WaitForEnd(timeout, SourceContext.Current.Token);

	public static ValueTask WaitForEnd<T>(this IFeedRecorder<T> recorder, CancellationToken ct)
		=> recorder.WaitForEnd(FeedRecorder.DefaultTimeout, ct);
}

public static class F<T>
{
	public static FeedRecorder<TFeed, T> Record<TFeed>(
		Func<IFeedRecorder<T>, TFeed> feedFactory, 
		SourceContext? context = null, 
		bool autoEnable = true,
		[CallerArgumentExpression("feedFactory")] string? feedExpression = null,
		[CallerMemberName] string? memberName = null,
		[CallerLineNumber] int line = -1)
		where TFeed : IFeed<T>
		=> new(feedFactory, context ?? SourceContext.Current, autoEnable, feedExpression ?? $"{memberName}@{line}");
}
