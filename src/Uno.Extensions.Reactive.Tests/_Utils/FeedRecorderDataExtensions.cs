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
		internal static async Task WaitForData<T>(this IFeedRecorder<T> recorder, Func<T, bool> predicate, CancellationToken ct = default)
		{
			while (!LastDataMatches(recorder, predicate))
			{
				await recorder.WaitForMessages(recorder.Count + 1, FeedRecorder.DefaultTimeout, ct);
			}
		}

		private static bool LastDataMatches<T>(IFeedRecorder<T> recorder, Func<T, bool> predicate)
			=> recorder.Count > 0
				&& recorder[recorder.Count - 1].Current.Data.IsSome(out var data)
				&& predicate(data);
	}
}
