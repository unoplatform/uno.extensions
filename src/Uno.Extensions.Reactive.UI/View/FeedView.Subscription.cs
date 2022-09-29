using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;
#if WINUI
using _Page = Microsoft.UI.Xaml.Controls.Page;
#else
using _Page = Windows.UI.Xaml.Controls.Page;
#endif

namespace Uno.Extensions.Reactive.UI;

public partial class FeedView
{
	private class Subscription : IDisposable
	{
		private readonly CancellationTokenSource _ct = new();
		private readonly RequestSource _requests = new();
		private readonly TokenSetAwaiter<RefreshToken> _refresh = new();
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
				// When feed changes, we consider us as loading (but only until we get the first non transient value)
				_view.SetIsLoading(true);

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
						_view.SetIsLoading(false);
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

				if (_view.VisualStateSelector?.GetVisualStates(_view, message).ToList() is { Count: > 0 } visualStates)
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
			} while (elt is not _Page and not null);

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
