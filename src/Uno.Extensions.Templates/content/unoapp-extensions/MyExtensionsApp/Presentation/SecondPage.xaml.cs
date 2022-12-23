namespace MyExtensionsApp.Presentation;

public sealed partial class SecondPage : Page
{
	public SecondPage()
	{
//+:cnd:noEmit
#if use-csharp-markup
#if(reactive)
		this.DataContext<BindableSecondModel>((page, vm) => page
#else
		this.DataContext<SecondViewModel>((page, vm) => page
#endif
			.Background(Theme.Brushes.Background.Default)
			.Content(new Grid().Children(
				new NavigationBar()
					.Content("Second Page")
					.MainCommand(new AppBarButton()
						.Icon(new BitmapIcon().UriSource(new Uri("ms-appx:///Assets/Icons/back.png")))
					),
				new TextBlock()
					.Text(() => vm.Entity.Name)
					.HorizontalAlignment(HorizontalAlignment.Center)
					.VerticalAlignment(VerticalAlignment.Center))));
#else
		this.InitializeComponent();
#endif
	}
}

