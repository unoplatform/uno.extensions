using Uno.Toolkit.UI;

namespace MyExtensionsApp.Views;

public sealed partial class Shell : UserControl, IContentControlProvider
{
	public Shell()
	{
//+:cnd:noEmit
#if use-csharp-markup
		this.Content(
			new ExtendedSplashScreen()
				.Assign(out var splash)
				.HorizontalAlignment(HorizontalAlignment.Stretch)
				.VerticalAlignment(VerticalAlignment.Stretch)
				.HorizontalContentAlignment(HorizontalAlignment.Stretch)
				.VerticalContentAlignment(VerticalAlignment.Stretch)
				.LoadingContentTemplate<object>(_ => new Grid()
					.RowDefinitions(new GridLength(2, GridUnitType.Star), new GridLength(1, GridUnitType.Star))
					.Children(
						new ProgressRing()
							.Grid(row: 1)
							.VerticalAlignment(VerticalAlignment.Center)
							.HorizontalAlignment(HorizontalAlignment.Center)
							.Height(100)
							.Width(100)
					)
				)
			);
		Splash = splash;
#else
		this.InitializeComponent();
#endif
//-:cnd:noEmit
	}
//+:cnd:noEmit
#if use-csharp-markup

	private ExtendedSplashScreen Splash { get; }
#endif
//-:cnd:noEmit

	public ContentControl ContentControl => Splash;
}
