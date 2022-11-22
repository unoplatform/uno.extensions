namespace TestHarness;

public sealed partial class App : Application
{
	private Window? _window;
	public Window Window => _window!;

	public App()
	{
		this.InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
#if WINDOWS
		// This is only required because we don't run UnoHost.CreateDefaultHost until a
		// test scenario is selected. This line is included to ensure web auth test cases
		// work, without having to navigate to a test scenario in the new app instance
		// that is launched to handle the web auth redirect
		WinUIEx.WebAuthenticator.Init();
#endif


#if NET6_0_OR_GREATER && WINDOWS
				_window = new Window();
#else
		_window = Window.Current;
#endif



		var rootFrame = _window.Content as TestFrameHost;

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (rootFrame == null)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new TestFrameHost();


			// Place the frame in the current Window
			_window.Content = rootFrame;
		}
		_window.Activate();



	}
}
