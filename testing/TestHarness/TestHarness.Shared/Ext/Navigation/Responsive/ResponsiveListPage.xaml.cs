
namespace TestHarness.Ext.Navigation.Responsive;

public sealed partial class ResponsiveListPage : Page
{
	public ResponsiveListPage()
	{
		this.InitializeComponent();
	}

	private string? currentState;
	private void PageSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var threshold = App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0;
		var newState = e.NewSize.Width > threshold ? nameof(Wide) : nameof(Narrow);
		if(currentState != newState)
		{
			currentState = newState;
			VisualStateManager.GoToState(this, newState, true);
		}
	}
}
