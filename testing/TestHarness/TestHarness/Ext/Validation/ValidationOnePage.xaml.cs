

namespace TestHarness.Ext.Navigation.Validation
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class ValidationOnePage : Page
	{
		public ValidationOneViewModel? ViewModel => DataContext as ValidationOneViewModel;
		public ValidationOnePage()
		{
			this.InitializeComponent();
		}
	}
}
