namespace MyExtensionsApp._1.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
	public Shell()
	{
//+:cnd:noEmit
#if useCsharpMarkup
		this.Content(

#if useToolkit
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
#else
			new ContentControl()
				.Assign(out var splash)
				.HorizontalAlignment(HorizontalAlignment.Stretch)
				.VerticalAlignment(VerticalAlignment.Stretch)
				.HorizontalContentAlignment(HorizontalAlignment.Stretch)
				.VerticalContentAlignment(VerticalAlignment.Stretch)
#endif
			);
		Splash = splash;
#else
		this.InitializeComponent();
#endif
//-:cnd:noEmit
	}
//+:cnd:noEmit
#if useCsharpMarkup

#if useToolkit
	private ExtendedSplashScreen Splash { get; }
#else
	private ContentControl Splash { get; }
#endif
#endif
//-:cnd:noEmit

	public ContentControl ContentControl => Splash;
}
