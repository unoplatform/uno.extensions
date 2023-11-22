namespace Uno.Extensions;

public static class PlatformHelper
{
	private const long AppModelErrorNoPackage = 15700L;

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);

	private static bool _isAppPackaged;
	private static bool _isNetCore;
	private static bool _initialized;
	private static bool _isWebAssembly;

	/// <summary>
	/// Determines if the platform is runnnig WebAssembly
	/// </summary>
	public static bool IsWebAssembly
	{
		get
		{
			EnsureInitialized();
			return _isWebAssembly;
		}
	}

	/// <summary>
	/// Determines if the current runtime is running on .NET Core or 5 and later
	/// </summary>
	public static bool IsNetCore
	{
		get
		{
			EnsureInitialized();
			return _isNetCore;
		}
	}

	/// <summary>
	/// Determines if the current runtime supports threading
	/// </summary>
	public static bool IsThreadingEnabled
	{ get; } = !IsWebAssembly || IsWebAssemblyThreadingSupported;

	private static bool IsWebAssemblyThreadingSupported { get; } = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_CONFIGURATION")?.StartsWith("threads", StringComparison.OrdinalIgnoreCase) ?? false;

	/// <summary>
	/// Gets a value indicating whether the app is packaged.
	/// </summary>
	public static bool IsAppPackaged
	{
		get
		{
			EnsureInitialized();
			return _isAppPackaged;
		}
	}

	/// <summary>
	/// Initialization is performed explicitly to avoid a mono/mono issue regarding .cctor and FullAOT
	/// see https://github.com/unoplatform/uno/issues/5395
	/// </summary>
	private static void EnsureInitialized()
	{
		if (!_initialized)
		{
			_initialized = true;

			_isNetCore = Type.GetType("System.Runtime.Loader.AssemblyLoadContext") != null;

			// Origin of the value : https://github.com/mono/mono/blob/a65055dbdf280004c56036a5d6dde6bec9e42436/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L115
			_isWebAssembly =
				RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")) // Legacy Value (Bootstrapper 1.2.0-dev.29 or earlier).
				|| RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));

			// If wasm, then can assume app isn't packaged, so skip this check
			if (!IsWebAssembly)
			{
				try
				{
					// Application is MSIX packaged if it has an identity: https://learn.microsoft.com/en-us/windows/msix/detect-package-identity
					int length = 0;
					var sb = new System.Text.StringBuilder(0);
					int result = GetCurrentPackageFullName(ref length, sb);
					_isAppPackaged = result != AppModelErrorNoPackage;
				}
				catch
				{
					_isAppPackaged = false;
				}
			}
		}
	}
}
