

namespace TestHarness
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application
	{
		private Window? _window;
		public Window Window => _window!;

		public App()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="args">Details about the launch request and process.</param>
		protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
		{


#if NET6_0_OR_GREATER && WINDOWS
			_window = new Window();
#else
			_window = Microsoft.UI.Xaml.Window.Current;
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
}
