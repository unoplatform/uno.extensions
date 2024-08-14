using Uno.UITest.Helpers.Queries;

namespace TestHarness.UITest;

public class Constants
{
	public readonly static string WebAssemblyDefaultUri = "https://localhost:57208";
	public readonly static string iOSAppName = "com.companyname.TestHarness";
	public readonly static string AndroidAppName = "com.companyname.TestHarness";
	public readonly static string iOSDeviceNameOrId = "iPad Pro (12.9-inch) (4th generation)";

	public readonly static Platform CurrentPlatform = Platform.Android;
	public const int DefaultPixelTolerance = 100;
}
