namespace TestHarness;

public sealed partial class TestFrameHost : UserControl
{
	public TestFrameHost()
	{
		this.InitializeComponent();
	}

	public void ExitTestClick(object sender, RoutedEventArgs e)
	{
		var page = TestFrame.Content as IDisposable;
		if(page is not null)
		{
			page.Dispose(); // Calls Stop on the host
		}
		this.TestFrame.GoBack();
	}
}
