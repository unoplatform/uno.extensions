namespace Uno.Extensions.Hosting;

internal record ApplicationBuilder(Application App, LaunchActivatedEventArgs Arguments) : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder>> _delegates = new List<Action<IHostBuilder>>();

	public Window Window { get; } =
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
		new Window();
#else
		Microsoft.UI.Xaml.Window.Current;
#endif

	public IHost Build()
	{
		var builder = UnoHost.CreateDefaultBuilder();
		foreach (var del in _delegates)
		{
			del(builder);
		}

		return builder.Build();
	}

	public IApplicationBuilder Configure(Action<IHostBuilder> configureHost)
	{
		_delegates.Add(configureHost);
		return this;
	}
}
