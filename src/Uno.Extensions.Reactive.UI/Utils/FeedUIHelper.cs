using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.UI.Utils;

/// <summary>
/// Helpers to work with feed from the UI thread.
/// </summary>
internal static class FeedUIHelper
{
	/// <summary>
	/// Get the source of a feed
	/// </summary>
	/// <remarks>Ensures that everything is dispatched on background thread (GetSource and enumeration of the source itself).</remarks>
	/// <param name="feed">The feed to get the source for.</param>
	/// <param name="context">The context to use to get the source.</param>
	/// <returns>The async enumeration of items produced by this signal optimized to be UI friendly (i.e. do the less work possible on UI thread).</returns>
	public static async IAsyncEnumerable<IMessage> GetSource(ISignal<IMessage> feed, SourceContext context)
	{
		var ct = context.Token;
		var enumerator = await GetSourceEnumerator(feed, context).ConfigureAwait(false);
		while (!ct.IsCancellationRequested && await MoveNext(enumerator, ct).ConfigureAwait(false))
		{
			yield return enumerator.Current;
		}
	}

	/// <summary>
	/// Get the source of a feed.
	/// </summary>
	/// <remarks>Ensures that everything is dispatched on background thread (GetSource and enumeration of the source itself).</remarks>
	/// <param name="feed">The feed to get the source for.</param>
	/// <param name="context">The context to use to get the source.</param>
	/// <returns>The async enumeration of items produced by this signal optimized to be UI friendly (i.e. do the less work possible on UI thread).</returns>
	public static async IAsyncEnumerable<Message<T>> GetSource<T>(IFeed<T> feed, SourceContext context)
	{
		var ct = context.Token;
		var enumerator = await GetSourceEnumerator(feed, context).ConfigureAwait(false);
		while (!ct.IsCancellationRequested && await MoveNext(enumerator, ct).ConfigureAwait(false))
		{
			yield return enumerator.Current;
		}
	}

	private static async ValueTask<IAsyncEnumerator<T>> GetSourceEnumerator<T>(ISignal<T> signal, SourceContext context)
	{
		var ct = context.Token;
		var src = DispatcherHelper.HasThreadAccess
			? await Task.Run(() => signal.GetSource(context, ct), ct).ConfigureAwait(false)
			: signal.GetSource(context, ct);

		return src.GetAsyncEnumerator(ct);
	}

	private static async ValueTask<bool> MoveNext<T>(IAsyncEnumerator<T> enumerator, CancellationToken ct)
	{
		if (DispatcherHelper.HasThreadAccess)
		{
			return await Task.Run(() => enumerator.MoveNextAsync().AsTask(), ct).ConfigureAwait(false);
		}
		else
		{
			return await enumerator.MoveNextAsync().ConfigureAwait(false);
		}
	}
}
