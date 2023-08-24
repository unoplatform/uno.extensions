using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Logging;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.Extensions.Reactive.Core.HotReload.HotReloadService))]

namespace Uno.Extensions.Reactive.Core.HotReload;

internal static class HotReloadService
{
	private static ILogger _log = typeof(HotReloadService).Log();

	internal static void ClearCache(Type[]? types)
	{
	}

	internal static void UpdateApplication(Type[]? types)
	{
		if (types is null or { Length: 0 })
		{
			return;
		}

		foreach (var type in types)
		{
			// Search for updated model types
			if (type.GetCustomAttribute<ModelAttribute>() is { Bindable: not null } model
				&& type.GetCustomAttribute<MetadataUpdateOriginalTypeAttribute>() is { OriginalType : not null } typeUpdate)
			{
				if (_log.IsEnabled(LogLevel.Information)) _log.Info($"Hot-patching bindable {typeUpdate.OriginalType} with {type}.");

				BindableViewModelBase.HotPatch(model.Bindable, typeUpdate.OriginalType, type);
			}
		}
	}
}
