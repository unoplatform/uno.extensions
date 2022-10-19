namespace Uno.Extensions.Navigation.Toolkit;

internal record LoadingTask(Task NavigationTask, FrameworkElement Context) : Uno.Toolkit.ILoadable
{
	private bool callbackConnected;
	public bool IsExecuting
	{
		get
		{
			Init();

			return !NavigationTask.IsCompleted;
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
				dispatcher?.ExecuteAsync(async () =>
				{
					IsExecutingChanged?.Invoke(this, EventArgs.Empty);
				});
			});
		}
	}


	public event EventHandler? IsExecutingChanged;
}
