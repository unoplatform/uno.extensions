using System;
using System.Linq;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace FluentAssertions;

public static class FeedFluentAssertionsExtensions
{
	public static MessageRecorderAssertions<T> Should<T>(this IFeedRecorder<T> feeds)
		=> new(feeds);

	public static MessageAssertions<T> Should<T>(this Message<T> message)
		=> new(message);

	public static MessageEntryAssertions<T> Should<T>(this MessageEntry<T> entry)
		=> new(entry);
}
