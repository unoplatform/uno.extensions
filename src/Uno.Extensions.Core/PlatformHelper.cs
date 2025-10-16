using System.Reflection;

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

	private static Assembly? _appAssembly;

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
	///  Set the value returned by <see cref="GetAppAssembly"/>.
	/// </summary>
	/// <param name="assembly">
	///  The <see cref="Assembly"/> that subsequent calls to <see cref="GetAppAssembly" /> should return.
	/// </param>
	/// <remarks>
	///   <para>If <paramref name="assembly"/> is <see langword="null" />, then
	///   <see cref="GetAppAssembly"/> will follow its default algorithm.
	///   </para>
	/// </remarks>
	public static void SetAppAssembly(Assembly? assembly) => _appAssembly = assembly;

#pragma warning disable RS0030
	/// <summary>
	///   Attempts to obtain the "App" <see cref="Assembly"/>.
	/// </summary>
	/// <returns>
	///   An <see cref="Assembly"/> if the "Application" assembly can be found; otherwise, <see langword="null"/>.
	/// </returns>
	/// <remarks>
	///   <para>
	///     If <see cref="SetAppAssembly(Assembly?)"/> is invoked before <c>GetAppAssembly()</c> is invoked, then
	///     <c>GetAppAssembly()</c> returns the value provided to <see cref="SetAppAssembly(Assembly?)"/>.
	///     If <see cref="SetAppAssembly(Assembly?)"/> was not invoked, <i>or</i> if <c>SetAppAssembly(null)</c> was
	///     invoked, then <c>GetAppAssembly</c> attempts to return a "useful" <see cref="Assembly"/> value.
	///   </para>
	///   <para>
	///     When <see cref="RuntimeFeature.IsDynamicCodeCompiled"/> is <see langword="false"/> and
	///     <see cref="RuntimeFeature.IsDynamicCodeSupported"/> is <see langword="false"/>, then
	///     <see cref="Assembly.GetEntryAssembly()"/> is returned.
	///     Otherwise, <see cref="Assembly.GetCallingAssembly()"/> is returned.  If <see cref="Assembly.GetCallingAssembly()"/>
	///     throws, then <see cref="Assembly.GetEntryAssembly()"/> is returned.
	///   </para>
	///   <block subset="none" type="note">
	///     <para>The complications are NativeAOT and Android, within which:</para>
	///     <list type="bullet">
	///       <item><term>
	///         <see cref="Assembly.GetCallingAssembly()"/>: NativeAOT throws <see cref="PlatformNotSupportedException"/> <i>by default</i>;
	///         NativeAOT can instead return <see cref="Assembly.GetEntryAssembly()"/> if the
	///         <c>Switch.System.Reflection.Assembly.SimulatedCallingAssembly</c> runtime switch is <c>true</c>.
	///         Android with MonoVM and CoreCLR returns a non-<see langword="null"/> value.
	///       </term></item>
	///       <item><term>
	///         <see cref="Assembly.GetEntryAssembly()"/>: NativeAOT returns a non-<see langword="null"/> value.
	///         Android with MonoVM and CoreCLR returns <see langword="null"/> (!).
	///       </term></item>
	///       <item><term>
	///         <see cref="Assembly.GetExecutingAssembly()"/>: NativeAOT always an exception (!).
	///         Android with MonoVM and CoreCLR returns a non-<see langword="null"/> value.
	///       </term></item>
	///     </list>
	///   </block>
	/// </remarks>
	public static Assembly? GetAppAssembly()
	{
		if (!RuntimeFeature.IsDynamicCodeCompiled && !RuntimeFeature.IsDynamicCodeSupported)
		{
			// Assume NativeAOT. Might also be iOS+FullAOT…?
			return _appAssembly ??= Assembly.GetEntryAssembly();
		}

		try
		{
			return _appAssembly ??= Assembly.GetCallingAssembly();
		}
		catch (Exception)
		{
			// Log?
			return _appAssembly ??= Assembly.GetEntryAssembly();
		}
#pragma warning restore RS0030
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
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				_isAppPackaged = IsWindowsAppPackaged();
			}
		}
	}

	private static bool IsWindowsAppPackaged()
	{
		try
		{
			// Application is MSIX packaged if it has an identity: https://learn.microsoft.com/en-us/windows/msix/detect-package-identity
			int length = 0;
			var sb = new System.Text.StringBuilder(0);
			int result = GetCurrentPackageFullName(ref length, sb);
			return result != AppModelErrorNoPackage;
		}
		catch
		{
			return false;
		}
	}
}
