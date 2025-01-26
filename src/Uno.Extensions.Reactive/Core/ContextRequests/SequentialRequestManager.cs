using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A manager of <see cref="IContextRequest{TToken}"/> that will issue a new one <typeparamref name="TToken"/> for each requests received,
/// no matter if the previous one have been completed or not.
/// </summary>
/// <typeparam name="TRequest">The type of the requests that manager handles.</typeparam>
/// <typeparam name="TToken">The type of tokens that are issue by this manager.</typeparam>
internal class SequentialRequestManager<TRequest, TToken> : IAsyncEnumerable<TokenSet<TToken>>
	where TRequest : IContextRequest<TToken>
	where TToken : class, IToken<TToken>
{
	private readonly AsyncEnumerableSubject<TokenSet<TToken>> _tokens = new(AsyncEnumerableReplayMode.EnabledForFirstEnumeratorOnly);
	private readonly CancellationToken _ct;

	private TToken _current;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="context">The subscription context for which this manager is used.</param>
	/// <param name="initial">The initial token.</param>
	/// <param name="ct">
	/// The cancellation token of the subscription to which this manager is linked.
	/// This manager will complete itself when this token is cancelled.
	/// </param>
	/// <param name="autoPublishInitial">
	/// Indicates if the <paramref name="initial"/> token should be published in the implementation of <see cref="IAsyncEnumerable{TToken}"/>.
	/// This is use-full when this manager is used as a trigger to do some work and we want to automatically do trigger that work on subscription.
	/// </param>
	public SequentialRequestManager(SourceContext context, TToken initial, CancellationToken ct, bool autoPublishInitial = true)
	{
		_current = initial;
		_ct = ct;

		if (autoPublishInitial)
		{
			_tokens.SetNext(initial);
		}

		context.Requests<TRequest>(OnRequest, ct);
		ct.Register(_tokens.TryComplete);
	}

	/// <summary>
	/// Gets the last request received.
	/// </summary>
	/// <remarks>Be aware that this request might not be in sync with the token when enumerating.</remarks>
	public TRequest? LastRequest { get; private set; }

	/// <summary>
	/// The current token that is being used to reply to received request.
	/// </summary>
	public TToken Current => _current;

	/// <inheritdoc />
	public IAsyncEnumerator<TokenSet<TToken>> GetAsyncEnumerator(CancellationToken ct)
		=> _tokens.GetAsyncEnumerator(CancellationTokenSource.CreateLinkedTokenSource(ct, _ct).Token);

	private void OnRequest(TRequest request)
	{
		if (_ct.IsCancellationRequested)
		{
			return;
		}

		while(true)
		{
			var current = _current;
			var next = current.Next();
			if (Interlocked.CompareExchange(ref _current, next, current) == current)
			{
				LastRequest = request;
				request.Register(next);

				_tokens.TrySetNext(next);

				return;
			}
		}
	}
}
