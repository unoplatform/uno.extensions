using Microsoft.Maui.Devices;

namespace MauiEmbedding.Business;
public record VibrationService(IVibration Vibration) : IVibrationService
{
	public Task VibrateAsync()
	{
		var durationInMilliseconds = 3000;
		Vibration.Vibrate(durationInMilliseconds);
		return Task.Delay(durationInMilliseconds);
	}
}
