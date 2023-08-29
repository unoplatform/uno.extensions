namespace MauiEmbedding.MauiControls;

public partial class SyncfusionDemoPage : ContentPage
{
	public SyncfusionDemoPage()
	{
		InitializeComponent();

		Loaded += SyncfusionDemoPage_Loaded;
	}

	private void SyncfusionDemoPage_Loaded(object sender, EventArgs e)
	{
		var pv = Window?.Handler?.PlatformView;
		var hpv = Handler?.PlatformView;

		var current = DeviceDisplay.Current;
	}
}
