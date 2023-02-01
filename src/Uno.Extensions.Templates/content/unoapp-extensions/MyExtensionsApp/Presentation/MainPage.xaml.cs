namespace MyExtensionsApp.Presentation;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
//+:cnd:noEmit
#if useCsharpMarkup
		this.DataContext<$mainDataContext$>((page, vm) => page
			.NavigationCacheMode(NavigationCacheMode.Required)
			.Background(Theme.Brushes.Background.Default)
			.Content(new Grid()
				.RowDefinitions<Grid>("Auto,*")
				.Children(
					new NavigationBar().Content(() => vm.Title),
					new StackPanel()
						.Grid(row: 1)
						.HorizontalAlignment(HorizontalAlignment.Center)
						.VerticalAlignment(VerticalAlignment.Center)
						.Children(
							new TextBox()
								.Text(x => x.Bind(() => vm.Name).Mode(BindingMode.TwoWay))
								.PlaceholderText("Enter your name:")
								.Margin(8),
							new Button()
								.Content("Go to Second Page")
								.AutomationProperties(automationId: "SecondPageButton")
								.Command(() => vm.GoToSecond)))));
#else
		this.InitializeComponent();
#endif
	}
}
