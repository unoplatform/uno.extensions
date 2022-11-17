namespace Uno.Extensions.Hosting;

internal class ApplicationBuilder : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder>> _delegates = new List<Action<IHostBuilder>>();
	public ApplicationBuilder(Application app, LaunchActivatedEventArgs arguments)
	{
		App = app;
		Arguments = arguments;

#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
		Window = new Window();
#else
        Window = Microsoft.UI.Xaml.Window.Current;
#endif
	}

	public Application App { get; }
	public LaunchActivatedEventArgs Arguments { get; }
	public Window Window { get; }

	public IHost Build()
	{
		var builder = UnoHost.CreateDefaultBuilder();
		foreach (var @delegate in _delegates)
		{
			@delegate(builder);
		}

		// TODO: Need to expose the "enableUnoLogging" as part of UseLogging
		return builder.Build();
	}

	public IApplicationBuilder Configure(Action<IHostBuilder> configureHost)
	{
		_delegates.Add(configureHost);
		return this;
	}
}
