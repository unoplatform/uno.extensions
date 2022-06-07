
namespace TestHarness.Ext.Navigation.Responsive;

public sealed partial class ResponsiveListPage : Page
{
	public ResponsiveListPage()
	{
		this.InitializeComponent();

		Loaded += ResponsiveListPage_Loaded;
	}

	private void ResponsiveListPage_Loaded(object sender, RoutedEventArgs e) => Resize(this.ActualWidth, true);

	private string? currentState;
	private void PageSizeChanged(object sender, SizeChangedEventArgs e)
	{
		Resize(e.NewSize.Width);
	}
	private async Task Resize(double newWidth, bool refresh = false)
	{

		if (newWidth <= 0)
		{
			return;
		}


		var threshold = App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0;
		var newState = newWidth > threshold ? nameof(Wide) : nameof(Narrow);
		if (currentState != newState || refresh)
		{
			currentState = newState;
			// Task.Yield is required as we're setting visual states in code. If the
			// Region.Attached property is set to fast, it doesn't connect up the region
			// correctly. This isn't an issue when the visual states are driven using
			// adaptive triggers
			await Task.Yield();  
			VisualStateManager.GoToState(this, newState, true);
		}
	}
}
