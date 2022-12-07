//-:cnd:noEmit

namespace MyExtensionsApp.Views;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
//+:cnd:noEmit
#if use-csharp-markup
		this.Content(new Grid().Children(
			new TextBlock()
				.Text("Hello World!")
				.HorizontalAlignment(HorizontalAlignment.Center)
				.VerticalAlignment(VerticalAlignment.Center)
		));
#else
		this.InitializeComponent();
#endif
//-:cnd:noEmit
	}
}
