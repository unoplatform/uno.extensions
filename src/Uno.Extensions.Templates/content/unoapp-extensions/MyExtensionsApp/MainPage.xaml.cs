//-:cnd:noEmit

namespace MyExtensionsApp;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
//+:cnd:noEmit
#if use-csharp-markup
		this.Content(new Grid().Children(
			new TextBlock()
				.Grid(row: 0, column: 0)
				.Text("Hello Uno Platform!")
				.HorizontalAlignment(HorizontalAlignment.Center)
				.VerticalAlignment(VerticalAlignment.Center)
		));
#else
		this.InitializeComponent();
#endif
//-:cnd:noEmit
	}
}
