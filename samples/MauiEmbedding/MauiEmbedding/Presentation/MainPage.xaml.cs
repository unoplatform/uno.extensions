namespace MauiEmbedding.Presentation;

public sealed partial class MainPage : Page
{
	public MainViewModel? Vm => DataContext as MainViewModel;

	public MainPage()
	{
		this.InitializeComponent();
	}
}
