namespace TestHarness.Ext.Navigation.NavigationView;

public sealed partial class NavigationViewDataRecipeDetailsPage : Page
    {
        public NavigationViewDataRecipeDetailsPage()
        {
            this.InitializeComponent();
        }
    }


public partial record NavigationViewDataRecipeDetailsViewModel(Recipe Recipe) { }
