using Uno.UITest.Helpers.Queries;

namespace TestHarness.UITest;

public class Constants
{
	public readonly static string WebAssemblyDefaultUri = "http://localhost:5000";
	public readonly static string iOSAppName = "uno.platform.extensions.demo";
	public readonly static string AndroidAppName = "uno.platform.extensions.demo";
	public readonly static string iOSDeviceNameOrId = "iPad Pro (12.9-inch) (4th generation)";

	public readonly static Platform CurrentPlatform = Platform.Browser;
	public const int DefaultPixelTolerance = 100;
}
