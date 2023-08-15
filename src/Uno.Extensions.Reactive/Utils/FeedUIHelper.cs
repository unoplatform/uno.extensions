using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// Helpers to work with feed from the UI thread.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class FeedUIHelper
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

	private static async ValueTask<IAsyncEnumerator<Message<T>>> GetSourceEnumerator<T>(IFeed<T> signal, SourceContext context)
	{
		var ct = context.Token;
		var src = DispatcherHelper.HasThreadAccess
			? await Task.Run(() => context.GetOrCreateSource(signal), ct).ConfigureAwait(false)
			: context.GetOrCreateSource(signal);

		return src.GetAsyncEnumerator(ct);
	}

	private static async ValueTask<IAsyncEnumerator<TMessage>> GetSourceEnumerator<TMessage>(ISignal<TMessage> signal, SourceContext context)
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
