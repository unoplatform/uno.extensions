namespace Uno.Extensions;

public static class PlatformHelper
{
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

	private static bool IsWebAssemblyThreadingSupported { get; } = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_CONFIGURATION").StartsWith("threads", StringComparison.OrdinalIgnoreCase);


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
		}
	}
}
