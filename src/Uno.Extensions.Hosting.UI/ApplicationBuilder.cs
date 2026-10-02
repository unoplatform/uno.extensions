using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Hosting;

internal record ApplicationBuilder : IApplicationBuilder
{
	private readonly List<Action<IHostBuilder, Window>> _delegates = [];
	private Window? _window;

	/// <summary>
	/// Fires when a new <see cref="ApplicationBuilder"/> is created.
	/// External hosts can subscribe to inject configuration
	/// (such as logging providers) before the app calls <see cref="Build"/>.
	/// </summary>
	public static event Action<IApplicationBuilder>? OnCreate;

	/// <summary>
	/// Fires at the start of <see cref="Build"/>, before delegates are executed.
	/// Provides a last-chance hook to inspect or modify the builder after all
	/// app-side Configure calls are done.
	/// </summary>
	public event Action<IApplicationBuilder>? OnBuild;

	public ApplicationBuilder(Application App, LaunchActivatedEventArgs Arguments, Assembly ApplicationAssembly)
	{
		this.App = App;
		this.Arguments = Arguments;
		this.ApplicationAssembly = ApplicationAssembly;

		OnCreate?.Invoke(this);
	}

	public Application App { get; }

	public LaunchActivatedEventArgs Arguments { get; }

	public Assembly ApplicationAssembly { get; }

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
		OnBuild?.Invoke(this);

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
