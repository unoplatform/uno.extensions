
namespace TestHarness.Ext.Navigation.NavigationView;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NavigationViewDataCookbookDetailsPage : Page
    {
        public NavigationViewDataCookbookDetailsPage()
        {
            this.InitializeComponent();
        }
    }

public partial record NavigationViewDataCookbookDetailsViewModel(CookBook Cookbook)
{
}
