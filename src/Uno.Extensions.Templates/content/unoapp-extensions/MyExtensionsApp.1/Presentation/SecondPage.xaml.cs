namespace MyExtensionsApp._1.Presentation;

public sealed partial class SecondPage : Page
{
	public SecondPage()
	{
//+:cnd:noEmit
#if useCsharpMarkup
		this.DataContext<$secondDataContext$>((page, vm) => page
			.Background(Theme.Brushes.Background.Default)
			.Content(new Grid().Children(
#if useToolkit				
				new NavigationBar()
					.Content("Second Page")
					.MainCommand(new AppBarButton()
						.Icon(new BitmapIcon().UriSource(new Uri("ms-appx:///MyExtensionsApp.1/Assets/Icons/back.png")))
					),
#else
				new TextBlock()
					.Text("Second Page")
					.HorizontalAlignment(HorizontalAlignment.Center)
#endif
				new TextBlock()
					.Text(() => vm.Entity.Name)
					.HorizontalAlignment(HorizontalAlignment.Center)
					.VerticalAlignment(VerticalAlignment.Center))));
#else
		this.InitializeComponent();
#endif
	}
}

