
namespace Commerce;

public sealed partial class ProfilePage : Page
{
	public ProfileViewModel? ViewModel { get; set; }

	public ProfilePage()
	{
		this.InitializeComponent();

		this.Loaded += (s, e) =>
		{
			// Initialize the toggle to the current theme.
			darkModeToggle.IsEnabled = false;
			darkModeToggle.IsOn = SystemThemeHelper.IsRootInDarkMode(XamlRoot);
			darkModeToggle.IsEnabled = true;
		};

		DataContextChanged += ProfilePage_DataContextChanged;
	}

	private void ProfilePage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as ProfileViewModel;
	}

	private void ToggleDarkMode()
	{
		if (darkModeToggle.IsEnabled)
		{
			SystemThemeHelper.SetRootTheme(XamlRoot, darkModeToggle.IsOn);
		}
	}
}
