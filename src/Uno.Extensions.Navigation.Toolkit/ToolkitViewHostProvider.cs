
namespace Uno.Extensions.Navigation.Toolkit;

internal class ToolkitViewHostProvider : IViewHostProvider
{
	public void Append(Task NavigationTask) => throw new NotImplementedException();

	public FrameworkElement CreateViewHost() => new LoadingView
	{
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch,
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch
	};

	public IDeferrable InitializeViewHost(FrameworkElement contentControl, Task InitialNavigation) {
		var loading = new LoadingTask(InitialNavigation, contentControl);

		var lv = contentControl as LoadingView;
		if(lv is not null)
		{
			lv.Source = loading;
		}

		return loading;
	}

	private record LoadingTask(Task NavigationTask, FrameworkElement Context) : Uno.Toolkit.ILoadable, IDeferrable
	{
		private List<(Deferral Deferral, Task Task)> _deferrals = new List<(Deferral, Task)>();

		public Deferral GetDeferral()
		{
			Deferral? d = null;
			var completion = new TaskCompletionSource<bool>();
			d = new Deferral(() =>
			{
				_deferrals.Remove(def => def.Deferral == d);
				completion.SetResult(true);
			});
			_deferrals.Add((d, completion.Task));
			return d;
		}

		private bool callbackConnected;
		public bool IsExecuting
		{
			get
			{
				Init();

				var completed = NavigationTask.IsCompleted;
				if (!completed && !callbackConnected)
				{
					callbackConnected = true;
					var dispatcher = new Dispatcher(Context);
					NavigationTask.ContinueWith(async t =>
					{
						dispatcher?.ExecuteAsync(async () =>
						{
							IsExecutingChanged?.Invoke(this, EventArgs.Empty);
						});
					});

				}
				return !completed;
			}
		}

		private void Init()
		{
			if (!callbackConnected)
			{
				callbackConnected = true;
				var dispatcher = new Dispatcher(Context);
				NavigationTask.ContinueWith(async t =>
				{
					while(_deferrals.Count > 0)
					{
						await _deferrals[0].Task;
					}

					dispatcher?.ExecuteAsync(async () =>
					{
						IsExecutingChanged?.Invoke(this, EventArgs.Empty);
					});
				});
			}
		}


		public event EventHandler? IsExecutingChanged;
	}
}
