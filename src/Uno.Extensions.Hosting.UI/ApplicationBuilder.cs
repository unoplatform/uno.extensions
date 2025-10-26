namespace Uno.Extensions.Hosting;

internal record ApplicationBuilder : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder, Window>> _delegates = [];

	public ApplicationBuilder(Application app, LaunchActivatedEventArgs arguments, Assembly applicationAssembly, Func<Window>? windowFactory = null)
	{
		App = app;
		Arguments = arguments;
		ApplicationAssembly = applicationAssembly;
		Window = windowFactory?.Invoke() ?? CreateDefaultWindow();
	}

	public Application App { get; }
	public LaunchActivatedEventArgs Arguments { get; }
	public Assembly ApplicationAssembly { get; }

	public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

	public Window Window { get; }

	private static Window CreateDefaultWindow() =>
#if (NET6_0_OR_GREATER && WINDOWS) || HAS_UNO_WINUI
		new Window();
#else
		Window.Current!;
#endif

	public IHost Build()
	{
		var builder = UnoHost.CreateDefaultBuilder(
  			ApplicationAssembly, 
  			// Skip the first argument which contains the executable path that causes
  			// issues in the CommandLine parser.
	 		Environment.GetCommandLineArgs().Skip(1).ToArray());
		foreach (var del in _delegates)
		{
			del(builder, Window);
		}

		return builder.Build();
	}

	public IApplicationBuilder Configure(Action<IHostBuilder> configureHost)
	{
		_delegates.Add((builder, window) => configureHost(builder));
		return this;
	}

	public IApplicationBuilder Configure(Action<IHostBuilder, Window> configureHost)
	{
		_delegates.Add(configureHost);
		return this;
	}
}
