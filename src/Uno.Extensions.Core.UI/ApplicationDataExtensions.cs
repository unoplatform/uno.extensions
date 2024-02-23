using System.Reflection;

namespace Uno.Extensions;

internal static class ApplicationDataExtensions
{
	private const string DefaultUnoAppName = "unoapp";

	public static string DataFolder()
	{
		var dataFolder = string.Empty;
		if (
#if !__WINDOWS__
			true
#else
					PlatformHelper.IsAppPackaged
#endif
		)
		{
			try
			{
				dataFolder = Windows.Storage.ApplicationData.Current?.LocalFolder?.Path ?? string.Empty;
			}
			catch
			{
				// This will throw an exception on WinUI if unpackaged, so dataFolder will be null
				// Can also be null on Linux FrameBuffer
			}
		}

		if (string.IsNullOrWhiteSpace(dataFolder))
		{
			var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultUnoAppName;
			dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), appName);
		}

		if (!string.IsNullOrWhiteSpace(dataFolder) &&
			!Directory.Exists(dataFolder))
		{
			Directory.CreateDirectory(dataFolder);
		}

		return dataFolder;
	}
}
