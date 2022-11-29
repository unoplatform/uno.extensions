//-:cnd:noEmit

namespace MyExtensionsApp.Views;

public sealed partial class SecondPage : Page
{
	public SecondPage()
	{
//+:cnd:noEmit
#if markup
#if(reactive)
		this.DataContext<BindableSecondModel>((page, vm) => page
#else
		this.DataContext<SecondModel>((page, vm) => page
#endif
			.Background(ThemeResource.Get<Brush>("BackgroundBrush"))
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
//-:cnd:noEmit
	}
}

