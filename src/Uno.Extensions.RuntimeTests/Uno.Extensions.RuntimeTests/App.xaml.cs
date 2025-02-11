using Uno.Extensions.Reactive.WinUI.Tests;
using Uno.Resizetizer;

namespace Uno.Extensions.RuntimeTests;
public partial class App : Application
{
	private static void ForceAssemblyLoading()
	{
		var reactive_UI_Tests = new Given_FeedView();
	}

	/// <summary>
	/// Initializes the singleton application object. This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	protected Window? MainWindow { get; private set; }

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		MainWindow = new Window();
#if DEBUG
		MainWindow.EnableHotReload();
#endif


		MainWindow.SetWindowIcon();

		ForceAssemblyLoading();

		MainWindow!.Content ??= new Uno.UI.RuntimeTests.UnitTestsControl();
		MainWindow!.Activate();
	}


	/// <summary>
	/// Configures global Uno Platform logging
	/// </summary>
	public static void InitializeLogging()
	{
#if DEBUG
		// Logging is disabled by default for release builds, as it incurs a significant
		// initialization cost from Microsoft.Extensions.Logging setup. If startup performance
		// is a concern for your application, keep this disabled. If you're running on the web or
		// desktop targets, you can use URL or command line parameters to enable it.
		//
		// For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

		var factory = LoggerFactory.Create(builder =>
		{
#if BROWSERWASM1_0_OR_GREATER
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
			builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
			builder.AddConsole();
#endif

			// Exclude logs below this level
			builder.SetMinimumLevel(LogLevel.Information);

			// Default filters for Uno Platform namespaces
			builder.AddFilter("Uno", LogLevel.Warning);
			builder.AddFilter("Windows", LogLevel.Warning);
			builder.AddFilter("Microsoft", LogLevel.Warning);

			// Generic Xaml events
			// builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

			// Layouter specific messages
			// builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

			// builder.AddFilter("Windows.Storage", LogLevel.Debug );

			// Binding related messages
			// builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

			// Binder memory references tracking
			// builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

			// DevServer and HotReload related
			// builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

			// Debug JS interop
			// builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
		});

		global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
		global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
	}
}
