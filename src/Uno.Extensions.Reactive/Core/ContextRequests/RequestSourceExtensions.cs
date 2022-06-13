using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

internal static class RequestSourceExtensions
{
	/// <summary>
	/// Send a refresh request to feed that has been subscribed using this subscriber context.
	/// WARNING Read threading consideration in remarks.
	/// </summary>
	/// <remarks>
	/// This is expected to be invoked from a background thread.
	/// Using this from the UI thread will result into an empty TokenCollection.
	/// This is due to the fact that the UI thread does not allow attached child tasks,
	/// driving the request to be re-scheduled on a background thread before flowing into the requests <see cref="IAsyncEnumerable{T}"/>.
	/// </remarks>
	/// <returns>
	/// A collection of <see cref="RefreshToken"/> which indicates the minimum version reflecting that refresh,
	/// for all source feeds that have been impacted by this request.
	/// </returns>
	public static TokenCollection<RefreshToken> RequestRefresh(this IRequestSource requests)
	{
		var request = new RefreshRequest();
		requests.Send(request);

		return request.GetResult();
	}

	/// <summary>
	/// Send a pagination request to (list) feed that has been subscribed using this subscriber context.
	/// WARNING Read threading consideration in remarks.
	/// </summary>
	/// <remarks>
	/// This is expected to be invoked from a background thread.
	/// Using this from the UI thread will result into an empty TokenCollection.
	/// This is due to the fact that the UI thread does not allow attached child tasks,
	/// driving the request to be re-scheduled on a background thread before flowing into the requests <see cref="IAsyncEnumerable{T}"/>.
	/// </remarks>
	/// <returns>
	/// A collection of <see cref="RefreshToken"/> which indicates the minimum version reflecting the load of the requested page,
	/// for all source feeds that have been impacted by this request.
	/// </returns>
	public static TokenCollection<PageToken> RequestMoreItems(this IRequestSource requests, uint count)
	{
		var request = new PageRequest(count);
		requests.Send(request);

		return request.GetResult();
	}
}


internal record PageRequest(uint Count) : IContextRequest, IContextRequest<PageToken>
{
	private ImmutableList<PageToken> _result = ImmutableList<PageToken>.Empty;

	public void Register(PageToken token)
		=> ImmutableInterlocked.Update(ref _result, (list, t) => list.Add(t), token);

	public TokenCollection<PageToken> GetResult()
		=> new(_result);
}

internal record PageToken(IPaginatedSource Source, uint RootContextId, uint SequenceId) : IToken, IToken<PageToken>
{
	/// <inheritdoc />
	object IToken.Source => Source;

	/// <summary>
	/// Creates the initial token for a refreshable feed.
	/// </summary>
	/// <param name="source">The refreshable source feed.</param>
	/// <param name="context">The context used to <see cref="ISignal{T}.GetSource"/> on the <paramref name="source"/>.</param>
	/// <returns>The initial refresh token where <see cref="SequenceId"/> is set to 0.</returns>
	/// <remarks>This token represents the initial load of the <paramref name="source"/> and should not be propagated.</remarks>
	public static PageToken Initial(IPaginatedSource source, SourceContext context) => new(source, context.RootId, 0);

	/// <summary>
	/// Atomatically increments a refresh token and returns it.
	/// </summary>
	/// <param name="token">The backing variable that has to be incremented.</param>
	/// <returns>The updated token.</returns>
	public static PageToken InterlockedIncrement(ref PageToken token)
	{
		while (true)
		{
			var current = token;
			var next = current.Next();

			if (Interlocked.CompareExchange(ref token, next, current) == current)
			{
				return next;
			}
		}
	}

	[Pure]
	public PageToken Next()
		=> this with { SequenceId = SequenceId + 1 };
}

///// <summary>
///// A collection of <see cref="PageToken"/>.
///// </summary>
///// <param name="Tokens">The tokens.</param>
//internal record PageTokenCollection(IImmutableList<PageToken> Tokens) : TokenCollection<PageToken>(Tokens)
//{
//	/// <summary>
//	/// Creates a new collection with a single item.
//	/// </summary>
//	public static implicit operator PageTokenCollection(PageToken version)
//		=> new(ImmutableList.Create(version));
//}

