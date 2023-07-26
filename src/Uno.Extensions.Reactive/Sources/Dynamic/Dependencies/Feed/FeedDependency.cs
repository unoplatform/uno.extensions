using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Sources;

internal abstract class FeedDependency
{
	private static readonly ILogger _log = typeof(FeedDependency).Log();

	public static async ValueTask<Message<T>?> TryGetCurrentMessage<T>(IFeed<T> feed)
		=> FeedExecution.Current is { } exec
			? await exec.Session.Feeds.Get(feed, exec).GetCurrentMessage(exec)
			: null;

	public static async ValueTask<Message<IImmutableList<T>>?> TryGetCurrentMessage<T>(IListFeed<T> feed)
		=> FeedExecution.Current is { } exec
			? await exec.Session.Feeds.Get(feed, exec).GetCurrentMessage(exec)
			: null;

	public static void NotifyTouched(IMessageEntry entry, MessageAxis axis)
	{
		if (FeedExecution.Current is { } exec)
		{
			if (exec.Session.Feeds.TryGet(entry, out var dependency))
			{
				dependency.NotifyTouched(exec, axis);
			}
			else if (_log.IsEnabled(LogLevel.Information))
			{
				_log.Info(
					$"Feed dependency not found for entry '{entry}' while a {nameof(FeedExecution)} is active. "
					+ $"This means that a feed is being used while loading '{exec.Session.Owner}' but without being registered as dependency, "
					+ $"so if that feed is being updated, the '{exec.Session.Owner}' won't be refreshed/reloaded.");
			}
		}
	}

	private protected FeedDependency(ISignal<IMessage> feed)
	{
		Feed = feed;
	}

	/// <summary>
	/// The dependency feed.
	/// (i.e. the feed on which the session depends, a new value pushed by this feed will cause update or a reload of the resulting <see cref="FeedSession.Owner"/>).
	/// </summary>
	internal ISignal<IMessage> Feed { get; }

	private protected abstract void NotifyTouched(FeedExecution execution, MessageAxis axis);
}
