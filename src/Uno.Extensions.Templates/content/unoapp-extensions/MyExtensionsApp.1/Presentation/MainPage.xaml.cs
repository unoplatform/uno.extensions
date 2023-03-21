namespace MyExtensionsApp._1.Presentation;

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
#if useToolkit					
					new NavigationBar().Content(() => vm.Title),
#else
				new TextBlock()
					.Text(() => vm.Title)
					.HorizontalAlignment(HorizontalAlignment.Center)
#endif
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
