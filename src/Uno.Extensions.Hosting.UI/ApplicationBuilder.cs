namespace Uno.Extensions.Hosting;

internal record ApplicationBuilder(Application App, LaunchActivatedEventArgs Arguments, Assembly ApplicationAssembly) : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder, Window>> _delegates = [];

	public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

	public Window Window { get; } =
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
		new Window();
#elif HAS_UNO_WINUI
		// Window.Current can be null with Uno.WinUI 5.2+
		// When updating Hosting to Uno.WinUI 5.2+ we need to remove the compiler directive
		Window.Current ?? new Window();
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
