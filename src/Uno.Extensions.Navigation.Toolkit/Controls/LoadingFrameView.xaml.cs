using Uno.Extensions.Navigation.UI.Controls;
using Uno.Toolkit;

namespace Uno.Extensions.Navigation.Toolkit.Controls;

public sealed partial class LoadingFrameView : BaseFrameView
{
	public LoadingFrameView()
	{
		this.InitializeComponent();

		var loadable = new FrameLoadingView(NavigationFrame);
		LV.Source = loadable;
	}

	public override INavigator? Navigator => NavFrame.Navigator();

	public override Frame NavigationFrame => NavFrame;


	private record FrameLoadingView(Frame NavigationFrame) : Uno.Toolkit.ILoadable
	{
		public bool IsExecuting
		{
			get
			{
				Connect();
				return NavigationFrame.Content is null ||
					!((NavigationFrame.Content as FrameworkElement)?.IsLoaded??true);
			}
		}

		private bool isConnected = false;
		private void Connect()
		{
			if (isConnected)
			{
				return;
			}
			isConnected = true;
			NavigationFrame.Navigated += NavigationFrame_Navigated;
		}

		private async void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
		{
			NavigationFrame.Navigated -= NavigationFrame_Navigated;
			if (NavigationFrame.Content is FrameworkElement fe &&
				!fe.IsLoaded)
			{
				fe.Loaded += Fe_Loaded;
			}
			else
			{
				IsExecutingChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		private void Fe_Loaded(object sender, RoutedEventArgs e)
		{
			IsExecutingChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler? IsExecutingChanged;
	}
}
