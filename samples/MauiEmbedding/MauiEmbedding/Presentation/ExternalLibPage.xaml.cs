namespace MauiEmbedding.Presentation;

public sealed partial class ExternalLibPage : Page
{
	public MainViewModel? Vm => DataContext as MainViewModel;

	public ExternalLibPage()
	{
		this.InitializeComponent();
	}
}
