namespace Playground.Services
{
	public class SimpleStartupService:IHostedService, IStartupService
	{
		private TaskCompletionSource<bool> _completion = new TaskCompletionSource<bool>();
		public Task StartAsync(CancellationToken cancellationToken)
		{
			_completion.SetResult(true);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		public Task StartupComplete()
		{
			return _completion.Task;
		}


	}
}
