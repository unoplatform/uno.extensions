using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.UI;

public partial class FeedView
{
	private class Subscription : IDisposable
	{
		private readonly CancellationTokenSource _ct = new();
		private readonly RequestSource _requests = new();
		private readonly TokenCompletionHandler<RefreshToken> _refresh = new();
		private readonly FeedView _view;
		private readonly VisualStateHelper _visualStateManager;

		public ISignal<IMessage> Feed { get; }

		public Subscription(FeedView view, ISignal<IMessage> feed)
		{
			_view = view;
			Feed = feed;

			_visualStateManager = new VisualStateHelper(_view);
			_ = Enumerate();
		}

		public bool RequestRefresh(Action completionAction)
			=> _refresh.WaitFor(_requests.RequestRefresh(), completionAction);

		private async Task Enumerate()
		{
			try
			{
				// Note: Here we expect the Feed to be an IState, so we use the Feed.GetSource instead of ctx.GetOrCreateSource().
				//		 The 'ctx' is provided only for safety to improve caching, but it's almost equivalent to SourceContext.None
				//		 (especially when using SourceContext.GetOrCreate(_view)).

				var ctx = SourceContext.Find(_view.DataContext)
					?? SourceContext.Find(FindPage()?.DataContext)
					?? SourceContext.GetOrCreate(_view);

				await foreach (var message in Feed.GetSource(ctx.CreateChild(_requests), _ct.Token).WithCancellation(_ct.Token).ConfigureAwait(true))
				{
					Update(message);

					if (!message.Current.IsTransient)
					{
						_refresh.Received(message.Current.Get(MessageAxis.Refresh));
					}
				}
			}
			catch (Exception error)
			{
				this.Log().Error(error, "Subscription to feed failed, view will no longer render updates made by the VM.");
			}
		}

		private void Update(IMessage message)
		{
			try
			{
				_view.State.Update(message);

				if (_view.VisualStateSelector?.GetVisualStates(message).ToList() is { Count: > 0 } visualStates)
				{
					foreach (var state in visualStates)
					{
						_visualStateManager.GoToState(state.stateName, state.shouldUseTransition);
					}
				}
			}
			catch (Exception error)
			{
				this.Log().Error(error, "Failed to change visual state.");
			}
		}

		private FrameworkElement? FindPage()
		{
			var elt = _view as FrameworkElement;
			do
			{
				elt = VisualTreeHelper.GetParent(elt) as FrameworkElement;
			} while (elt is not Page and not null);

			return elt;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_ct.Cancel();
			_requests.Dispose();
			_refresh.Dispose();
		}
	}
}

internal class TokenCompletionHandler<T> : IDisposable
	where T : IToken
{
	private TokenCollection<T>? _lastReceived;
	private bool _isDisposed;

	private event EventHandler<TokenCollection<T>?>? ReceivedTokens;

	public void Received(TokenCollection<T>? tokens)
	{
		if (_isDisposed || tokens is null)
		{
			return;
		}

		_lastReceived = tokens;
		ReceivedTokens?.Invoke(this, tokens);
	}

	public Task WaitFor(TokenCollection<T> tokens, CancellationToken ct)
	{
		if (_isDisposed || tokens.IsEmpty)
		{
			return Task.CompletedTask;
		}

		var task = new TaskCompletionSource<Unit>();
		var request = new Request(this, tokens, () => task.TrySetResult(default));
		ct.Register(request.Complete);

		return task.Task;
	}

	/// <summary>
	/// Waits for a minimum version of tokens.
	/// </summary>
	/// <param name="tokens">The minimum version of tokens we are waiting for.</param>
	/// <param name="completionAction">The action to execute when the <paramref name="tokens"/> has been received.</param>
	/// <returns>True is the tokens are awaited, false if the tokens are already present and <paramref name="completionAction"/> has already been invoked.</returns>
	public bool WaitFor(TokenCollection<T> tokens, Action completionAction)
	{
		if (_isDisposed || tokens.IsEmpty)
		{
			return false;
		}

		return !new Request(this, tokens, completionAction).IsCompleted;
	}

	private class Request
	{
		private readonly TokenCompletionHandler<T> _owner;
		private readonly TokenCollection<T> _tokens;

		private Action? _completion;

		public bool IsCompleted { get; private set; }

		public Request(TokenCompletionHandler<T> owner, TokenCollection<T> tokens, Action completion)
		{
			_owner = owner;
			_tokens = tokens;
			_completion = completion;

			Enable(owner, tokens);
		}

		private void Enable(TokenCompletionHandler<T> owner, TokenCollection<T> tokens)
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

		private void OnTokenReceived(object? _, TokenCollection<T>? receivedTokens)
		{
			// If receivedTokens is null, it means that the Owner ahs been disposed
			if (receivedTokens?.IsGreaterOrEquals(_tokens) ?? true)
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
