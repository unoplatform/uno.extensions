//-:cnd:noEmit
namespace MyExtensionsApp;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
//+:cnd:noEmit
#if use-csharp-markup
		this.Content(new StackPanel()
			.VerticalAlignment(VerticalAlignment.Center)
			.HorizontalAlignment(HorizontalAlignment.Center)
			.Children(
				new TextBlock()
					.Text("Hello Uno Platform!")
			));
#else
		this.InitializeComponent();
#endif
//-:cnd:noEmit
	}
}
