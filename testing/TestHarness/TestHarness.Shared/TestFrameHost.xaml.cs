namespace TestHarness;

public sealed partial class TestFrameHost : UserControl
{
	public TestFrameHost()
	{
		this.InitializeComponent();
	}

	public void ExitTestClick(object sender, RoutedEventArgs e)
	{
		this.TestFrame.GoBack();
	}
}
