namespace Playground.Services;

public class NeedsADispatcherService
{
	private readonly IDispatcher _dispatcher;
	public NeedsADispatcherService(IDispatcher dispatcher)
	{
		_dispatcher = dispatcher;
	}


	public async Task<string> RunSomethingWithDispatcher()
	{
		await Task.Delay(1000).ConfigureAwait(false);

		return await _dispatcher.ExecuteAsync(async ct =>
		{
			await Task.Delay(1000);
			return "Hi from UI thread";
		});

	}
}
