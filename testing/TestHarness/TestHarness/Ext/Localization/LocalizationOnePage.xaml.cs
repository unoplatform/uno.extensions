
namespace TestHarness.Ext.Navigation.Localization;

public sealed partial class LocalizationOnePage : Page
{
	public LocalizationOneViewModel? ViewModel => DataContext as LocalizationOneViewModel;
	public LocalizationOnePage()
	{
		this.InitializeComponent();
	}

}
