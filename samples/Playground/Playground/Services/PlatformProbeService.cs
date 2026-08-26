using System.Reflection;
using System.Runtime.Versioning;
using Uno.Extensions.Storage.KeyValueStorage;

namespace Playground.Services;

/// <summary>
/// Scratch diagnostic: logs which build of each Uno layer actually loaded and which
/// <see cref="IKeyValueStorage"/> the platform selection picked. Read it with
/// <c>adb logcat | Select-String PlatformProbe</c>. <see cref="Report"/> is also called
/// directly after the host is built, so the output does not depend on hosted services starting.
/// </summary>
public class PlatformProbeService(IServiceProvider services) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		Report(services, "hosted service");
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	public static void Report(IServiceProvider services, string stage)
	{
		try
		{
			Console.WriteLine($"[PlatformProbe] --- {stage} ---");

			// Resolve first so Uno.Extensions.Storage.UI is guaranteed to be loaded.
			var store = services.GetRequiredDefaultInstance<IKeyValueStorage>();

			var loaded = AppDomain.CurrentDomain.GetAssemblies();
			Describe(loaded.FirstOrDefault(a => a.GetName().Name == "Uno.Extensions.Storage.UI"));
			Describe(typeof(Microsoft.UI.Xaml.Window).Assembly);          // Uno.UI - native or Skia flavor
			Describe(typeof(Windows.Storage.ApplicationData).Assembly);   // Uno - WinRT layer
			Console.WriteLine($"[PlatformProbe] default IKeyValueStorage = {store.GetType().FullName}");
			Console.WriteLine($"[PlatformProbe] OperatingSystem: android={OperatingSystem.IsAndroid()} ios={OperatingSystem.IsIOS()} browser={OperatingSystem.IsBrowser()}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PlatformProbe] failed: {ex}");
		}
	}

	private static void Describe(Assembly? assembly)
	{
		if (assembly is null)
		{
			Console.WriteLine("[PlatformProbe] Uno.Extensions.Storage.UI is not loaded");
			return;
		}

		var name = assembly.GetName();
		var platform = assembly.GetCustomAttribute<TargetPlatformAttribute>()?.PlatformName ?? "(none - plain netX.0 build)";
		var refsUnoUI = assembly.GetReferencedAssemblies().Any(r => r.Name == "Uno.UI");
		Console.WriteLine($"[PlatformProbe] {name.Name} {name.Version}: platform={platform}, refsUnoUI={refsUnoUI}, location={assembly.Location}");
	}
}
