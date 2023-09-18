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

/// <summary>
/// Service responsible to handle hot reload events for the MVUX framework.
/// </summary>
internal static class HotReloadService
{
	private static ILogger _log = typeof(HotReloadService).Log();

	public static event Action<Type[]>? ApplicationUpdated;

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
				&& GetOriginalType(type) is { } originalType)
			{
				if (_log.IsEnabled(LogLevel.Information)) _log.Info($"Hot-patching bindable {originalType} with {type}.");

				BindableViewModelBase.HotPatch(model.Bindable, originalType, type);
			}
		}

		ApplicationUpdated?.Invoke(types);
	}

	// As the MetadataUpdateOriginalTypeAttribute might have been generated in the project, we have to use reflection instead of cannot use this:
	//&& type.GetCustomAttribute<MetadataUpdateOriginalTypeAttribute>() is { OriginalType : not null } typeUpdate)
	private static Type? GetOriginalType(Type type)
		=> type
			.GetCustomAttributes()
			.Select(attr =>
			{
				var attrType = attr.GetType();
				return attrType is { FullName: "System.Runtime.CompilerServices.MetadataUpdateOriginalTypeAttribute"}
					? attrType.GetProperty("OriginalType")?.GetValue(attr) as Type
					: null;
			})
			.FirstOrDefault(original => original is not null);
}
