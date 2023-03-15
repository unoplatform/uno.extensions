
using System.Collections.Immutable;

namespace TestHarness.Ext.Navigation.NavigationView;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NavigationViewDataRecipesPage : Page
    {
        public NavigationViewDataRecipesPage()
        {
            this.InitializeComponent();
        }
    }


public partial record NavigationViewDataRecipesViewModel(INavigationViewDataService Data) {

	public IListFeed<Recipe> Recipes => ListFeed.Async(async ct => Data.Recipes.ToImmutableList());

}
