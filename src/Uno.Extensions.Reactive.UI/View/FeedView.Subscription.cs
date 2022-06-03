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
		private readonly FeedView _view;
		private readonly VisualStateHelper _visualStateManager;
		
		private RefreshTokenCollection? _refresh;

		public ISignal<IMessage> Feed { get; }

		public Subscription(FeedView view, ISignal<IMessage> feed)
		{
			_view = view;
			Feed = feed;

			_visualStateManager = new VisualStateHelper(_view);
			_ = Enumerate();
		}

		public bool RequestRefresh()
			=> _requests.RequestRefresh() is { IsEmpty: false };

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

					if (!message.Current.IsTransient && _refresh is { } refresh && _refresh.IsLower(message))
					{
						_refresh = default;
						_view.Refresh.IsExecuting = false;
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
			_view.Refresh.IsExecuting = false;
		}
	}
}
