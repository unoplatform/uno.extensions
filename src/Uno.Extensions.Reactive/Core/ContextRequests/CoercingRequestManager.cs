using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A manager of <see cref="IContextRequest{TToken}"/> that will issue only one <typeparamref name="TToken"/>
/// for all requests received until source feed explicitly request to <see cref="MoveNext"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the requests that manager handles.</typeparam>
/// <typeparam name="TToken">The type of tokens that are issue by this manager.</typeparam>
internal class CoercingRequestManager<TRequest, TToken> : IAsyncEnumerable<TokenSet<TToken>>
	where TRequest : IContextRequest<TToken>
	where TToken : class, IToken<TToken>
{
	private readonly AsyncEnumerableSubject<TokenSet<TToken>> _tokens = new(AsyncEnumerableReplayMode.Disabled);
	private readonly CancellationToken _ct;
	private readonly bool _autoPublishInitial;

	private TToken _current;
	private TToken? _lastRequested;

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
	public CoercingRequestManager(SourceContext context, TToken initial, CancellationToken ct, bool autoPublishInitial = true)
	{
		_current = initial; // The page that is being loaded or will be load on next request
		_ct = ct;
		_autoPublishInitial = autoPublishInitial;

		if (autoPublishInitial)
		{
			_lastRequested = initial;
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
	public async IAsyncEnumerator<TokenSet<TToken>> GetAsyncEnumerator(CancellationToken ct = default)
	{
		if (_autoPublishInitial)
		{
			yield return _current;
		}

		await foreach (var token in _tokens.WithCancellation(ct).ConfigureAwait(false))
		{
			if (ct.IsCancellationRequested)
			{
				yield break;
			}

			yield return token;
		}
	}

	/// <summary>
	/// Move to the next token that is going to be used for subsequent request, if the current one has already been used.
	/// </summary>
	/// <remarks>
	/// The given token will be published by the <see cref="IAsyncEnumerable{T}"/> only on next request received.
	/// </remarks>
	/// <returns>`True` is the <see cref="Current"/> has been moved to next, `False` if the current has been kept (since it has not been used yet).</returns>
	public bool MoveNext()
	{
		if (_lastRequested == _current)
		{
			_current = _current.Next();
			return true;
		}
		else
		{
			return false;
		}
	}

	private void OnRequest(TRequest request)
	{
		if (_ct.IsCancellationRequested)
		{
			return;
		}

		LastRequest = request;
		request.Register(_current);

		if (Interlocked.Exchange(ref _lastRequested, _current) != _current)
		{
			_tokens.TrySetNext(_current);
		}
	}
}
