
namespace Commerce;

public sealed partial class ProfilePage : Page
{
	public ProfileViewModel? ViewModel { get; set; }

	public ProfilePage()
	{
		this.InitializeComponent();

		DataContextChanged += ProfilePage_DataContextChanged;
	}

	private void ProfilePage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as ProfileViewModel;
	}
}
