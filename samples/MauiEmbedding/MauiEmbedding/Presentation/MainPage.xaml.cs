namespace MauiEmbedding.Presentation;

public sealed partial class MainPage : Page
{
	public MainViewModel Vm => (MainViewModel)DataContext;

	public MainPage()
	{
		this.InitializeComponent();
		//new StackPanel().Spacing
	}
}
