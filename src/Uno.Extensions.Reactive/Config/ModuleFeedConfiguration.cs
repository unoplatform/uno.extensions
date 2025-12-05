using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Set of configuration options for the MVUX framework that are automatically pushed by module through.
/// Those method are expected to be invoked only by generated code (using module initializer attribute), you should not have to use any of them.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ModuleFeedConfiguration
{
	private static readonly ILogger _log = LogExtensions.Log<FeedConfiguration>();

#pragma warning disable RS0030
	private static readonly string? _entryAssembly = Assembly.GetEntryAssembly()?.GetName().Name;
#pragma warning restore RS0030

	/// <summary>
	/// Configures hot-reload for MVUX framework.
	/// </summary>
	/// <param name="module">Name of the module that is enabling the hot-reload support.</param>
	/// <param name="isEnabled"></param>
	/// <remarks>As hot-reload is expected to configured only by the application itself, this method reacts only on first call, and does nothing for all sub-sequent calls.</remarks>
	/// <remarks>Once enabled, hot-reload cannot be disabled.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void ConfigureHotReload(string module, bool isEnabled)
	{
		if (_entryAssembly?.Equals(module, StringComparison.OrdinalIgnoreCase) ?? false)
		{
			// Note: it's fine to directly use FeedConfiguration.HotReload as it's internal for now.
			// We should consider to have a dedicated flag otherwise (to not wipe the config of the user).
			FeedConfiguration.HotReload = isEnabled ? HotReloadSupport.Enabled : HotReloadSupport.Disabled;

			if (_log.IsEnabled(LogLevel.Information))
			{
				_log.Info($"Module '{module}' has {(isEnabled ? "enabled" : "disabled")} hot-reload support for MVUX.");
			}
		}
		else if (_log.IsEnabled(LogLevel.Information))
		{
			_log.Info($"Module '{module}' has requested to {(isEnabled ? "enabled" : "disabled")} hot-reload support for MVUX, but has hot-reload has already been configured by another module, request has been ignored..");
		}
	}
}
