using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Hosting;

internal record ApplicationBuilder(Application App, LaunchActivatedEventArgs Arguments, Assembly ApplicationAssembly) : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder, Window>> _delegates = [];
	private Window? _window;

	public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

	public Window Window
	{
		get => _window ??= CreateDefaultWindow();
		internal set => _window = value;
	}

	private static Window CreateDefaultWindow() =>
#if (NET6_0_OR_GREATER && WINDOWS) || HAS_UNO_WINUI
		new Window();
#else
		Window.Current!;
#endif

	[RequiresDynamicCode(UnoHost.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(UnoHost.RequiresUnreferencedCodeMessage)]
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
