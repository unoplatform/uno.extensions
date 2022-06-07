
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
	private void Resize(double newWidth, bool refresh = false)
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
			VisualStateManager.GoToState(this, newState, true);
		}
	}
}
