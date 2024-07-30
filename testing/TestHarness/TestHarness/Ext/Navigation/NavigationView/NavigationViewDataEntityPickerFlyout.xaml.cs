
using System.Collections.Immutable;
using Uno.Toolkit.UI;

namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewDataEntityPickerFlyout : Page
    {
        public NavigationViewDataEntityPickerFlyout()
        {
            this.InitializeComponent();
        }
    }


public partial record NavigationViewDataEntityPickerViewModel(INavigationViewDataService Data)
{
	public IListFeed<Recipe> Recipes => ListFeed.Async(async ct => Data.Recipes.ToImmutableList());


	public IListFeed<CookBook> Cookbooks => ListFeed.Async(async ct => Data.CookBooks.ToImmutableList());

}
