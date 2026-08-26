using System.Reflection;
using System.Runtime.Versioning;
using Uno.Extensions.Storage.KeyValueStorage;

namespace Playground.Services;

/// <summary>
/// Scratch diagnostic: logs which build of each Uno layer actually loaded and which
/// <see cref="IKeyValueStorage"/> the platform selection picked. Read it with
/// <c>adb logcat | Select-String PlatformProbe</c>.
/// </summary>
public class PlatformProbeService(IServiceProvider services) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken)
	{
		try
		{
			// Resolve first so Uno.Extensions.Storage.UI is guaranteed to be loaded.
			var store = services.GetRequiredDefaultInstance<IKeyValueStorage>();

			var loaded = AppDomain.CurrentDomain.GetAssemblies();
			Report(loaded.FirstOrDefault(a => a.GetName().Name == "Uno.Extensions.Storage.UI"));
			Report(typeof(Microsoft.UI.Xaml.Window).Assembly);          // Uno.UI - native or Skia flavor
			Report(typeof(Windows.Storage.ApplicationData).Assembly);   // Uno - WinRT layer
			Console.WriteLine($"[PlatformProbe] default IKeyValueStorage = {store.GetType().FullName}");
			Console.WriteLine($"[PlatformProbe] OperatingSystem: android={OperatingSystem.IsAndroid()} ios={OperatingSystem.IsIOS()} browser={OperatingSystem.IsBrowser()}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[PlatformProbe] failed: {ex}");
		}

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private static void Report(Assembly? assembly)
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
