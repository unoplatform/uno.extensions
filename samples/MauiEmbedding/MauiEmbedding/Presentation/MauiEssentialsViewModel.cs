using Microsoft.Maui.Devices;

namespace MauiEmbedding.Presentation;
partial class MauiEssentialsViewModel : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(BatteryLevelText))]
	double _batteryLevel;

	public string BatteryLevelText => $"Battery level: {BatteryLevel * 100}%";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DisplayInfo))]
	(double width, double height) _displaySize;

	public string DisplayInfo => $"Display info: {DisplaySize.width}x{DisplaySize.height}";

	public MauiEssentialsViewModel()
	{
		BatteryLevel = Battery.ChargeLevel;
		DisplaySize = (DeviceDisplay.MainDisplayInfo.Width, DeviceDisplay.MainDisplayInfo.Height);

		Battery.BatteryInfoChanged += OnBatteryBatteryInfoChanged;
		DeviceDisplay.MainDisplayInfoChanged += OnDeviceDisplayChanged;
	}

	private void OnDeviceDisplayChanged(object? sender, DisplayInfoChangedEventArgs e) =>
		DisplaySize = (e.DisplayInfo.Width, e.DisplayInfo.Height);
	private void OnBatteryBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e) =>
		BatteryLevel = e.ChargeLevel;
}
