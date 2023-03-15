
using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewDataCookbooksPage : Page
    {
        public NavigationViewDataCookbooksPage()
        {
            this.InitializeComponent();
        }
    }


public partial record NavigationViewDataCookbooksViewModel(INavigationViewDataService Data)
{

	public IListFeed<CookBook> Cookbooks=> ListFeed.Async(async ct => Data.CookBooks.ToImmutableList());

}
