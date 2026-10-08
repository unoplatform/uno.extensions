using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests
{
	internal static class FeedRecorderDataExtensions
	{
		/// <summary>
		/// Waits, message by message, until the last recorded message carries <paramref name="expected"/> as data.
		/// </summary>
		/// <exception cref="TimeoutException">No new message came within <see cref="FeedRecorder.DefaultTimeout"/>.</exception>
		internal static Task WaitForData<T>(this IFeedRecorder<T> recorder, T expected, CancellationToken ct = default)
			=> recorder.WaitForData(data => Equals(data, expected), ct);

		/// <summary>
		/// Waits, message by message, until the data of the last recorded message matches <paramref name="predicate"/>.
		/// </summary>
		/// <exception cref="TimeoutException">No new message came within <see cref="FeedRecorder.DefaultTimeout"/>.</exception>
		internal static Task WaitForData<T>(this IFeedRecorder<T> recorder, Func<T, bool> predicate, CancellationToken ct = default)
			=> recorder.WaitForMessage(message => message.Current.Data.IsSome(out var data) && predicate(data), ct);

		/// <summary>
		/// Waits, message by message, until the last recorded message matches <paramref name="predicate"/>.
		/// </summary>
		/// <exception cref="TimeoutException">No new message came within <see cref="FeedRecorder.DefaultTimeout"/>.</exception>
		internal static async Task WaitForMessage<T>(this IFeedRecorder<T> recorder, Func<Message<T>, bool> predicate, CancellationToken ct = default)
		{
			while (true)
			{
				// Read once: a message recorded after the check must not be skipped by waiting for one more.
				var count = recorder.Count;
				if (count > 0 && predicate(recorder[count - 1]))
				{
					return;
				}

				await recorder.WaitForMessages(count + 1, FeedRecorder.DefaultTimeout, ct);
			}
		}
	}
}
