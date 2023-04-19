using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// An helper class that ease awaiting a set of <see cref="IToken"/> from an active subscription on a <see cref="ISignal"/>.
/// </summary>
/// <typeparam name="T">The type of token managed by this class.</typeparam>
internal sealed class TokenSetAwaiter<T> : IDisposable
	where T : IToken
{
	private TokenSet<T>? _lastReceived;
	private bool _isDisposed;

	private event EventHandler<TokenSet<T>?>? ReceivedTokens;

	/// <summary>
	/// Notify that a set of token have been received on the active subscription.
	/// </summary>
	/// <param name="tokens"></param>
	public void Received(TokenSet<T>? tokens)
	{
		if (_isDisposed || tokens is null)
		{
			return;
		}

		_lastReceived = tokens;
		ReceivedTokens?.Invoke(this, tokens);
	}

	/// <summary>
	/// Asynchronously waits for a minimum set of tokens.
	/// </summary>
	/// <param name="tokens">The minimum set of tokens we are waiting for.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A task that will complete once the requested set of token has been received (cf. <see cref="Received"/>).</returns>
	public Task WaitFor(TokenSet<T> tokens, CancellationToken ct)
	{
		if (_isDisposed || tokens.IsEmpty)
		{
			return Task.CompletedTask;
		}

		var task = new TaskCompletionSource<Unit>();
		var awaiter = new Awaiter(this, tokens, () => task.TrySetResult(default));
		ct.Register(awaiter.Complete);

		return task.Task;
	}

	/// <summary>
	/// Waits for a minimum set of tokens.
	/// </summary>
	/// <param name="tokens">The minimum set of tokens we are waiting for.</param>
	/// <param name="completionAction">The action to execute when the <paramref name="tokens"/> has been received.</param>
	/// <returns>True is the tokens are awaited, false if the tokens are already present and <paramref name="completionAction"/> has already been invoked.</returns>
	public bool WaitFor(TokenSet<T> tokens, Action completionAction)
	{
		if (_isDisposed || tokens.IsEmpty)
		{
			return false;
		}

		return !new Awaiter(this, tokens, completionAction).IsCompleted;
	}

	private class Awaiter
	{
		private readonly TokenSetAwaiter<T> _owner;
		private readonly TokenSet<T> _tokens;

		private Action? _completion;

		public bool IsCompleted { get; private set; }

		public Awaiter(TokenSetAwaiter<T> owner, TokenSet<T> tokens, Action completion)
		{
			_owner = owner;
			_tokens = tokens;
			_completion = completion;

			Enable(owner, tokens);
		}

		private void Enable(TokenSetAwaiter<T> owner, TokenSet<T> tokens)
		{
			var lastReceived = owner._lastReceived;
			if (lastReceived?.IsGreaterOrEquals(tokens) ?? false)
			{
				Complete();
				return;
			}

			owner.ReceivedTokens += OnTokenReceived;

			// The _lastUpdated might have been already updated by the Received.
			// If so, we check it again.
			if (owner._isDisposed || (!ReferenceEquals(owner._lastReceived, lastReceived) && (owner._lastReceived?.IsGreaterOrEquals(tokens) ?? false)))
			{
				Complete();
			}
		}

		private void OnTokenReceived(object? _, TokenSet<T>? receivedTokens)
		{
			// If receivedTokens is null, it means that the Owner ahs been disposed
			if (receivedTokens is null || _tokens.IsLowerOrEquals(receivedTokens))
			{
				Complete();
			}
		}

		public void Complete()
		{
			IsCompleted = true;
			_owner.ReceivedTokens -= OnTokenReceived;
			Interlocked.Exchange(ref _completion, null)?.Invoke();
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		ReceivedTokens?.Invoke(this, null);
	}
}
