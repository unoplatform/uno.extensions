using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Logging;

#pragma warning disable IL2026  // 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Uno.Extensions.Reactive.Core.HotReload.HotReloadService))]
#pragma warning restore IL2026

namespace Uno.Extensions.Reactive.Core.HotReload;

/// <summary>
/// Service responsible to handle hot reload events for the MVUX framework.
/// </summary>
internal static class HotReloadService
{
	private static readonly ILogger _log = typeof(HotReloadService).CreateLog();
	private static readonly bool _trace = _log.IsEnabled(LogLevel.Trace);

	public static event Action<Type[]>? ApplicationUpdated;

	internal static void ClearCache(Type[]? types)
	{
	}

	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
	internal static void UpdateApplication(Type[]? types)
	{
		if (_trace) _log.Trace($"Received metadata updates for {types?.Length} types to be processed by MVUX hot-patch engine.");

		if (types is null or { Length: 0 })
		{
			return;
		}

		foreach (var type in types)
		{
			// Search for updated model types
			if (type.GetCustomAttribute<ModelAttribute>() is not { Bindable: not null } model)
			{
				if (_trace) _log.Trace($"Type {type.Name} is not a model (or has no bindable).");
				continue;
			}

			if (GetOriginalType(type) is not { } originalType)
			{
				if (_trace) _log.Trace($"Type {type.Name} doesn't have it original type defined, cannot process hot-patch.");
				continue;
			}

			if (_log.IsEnabled(LogLevel.Information)) _log.Info($"Hot-patching bindables of {originalType} to use the updated {type}.");

			BindableViewModelBase.HotPatch(model.Bindable, originalType, type);
		}

		ApplicationUpdated?.Invoke(types);
	}

	// As the MetadataUpdateOriginalTypeAttribute might have been generated in the project, we have to use reflection instead of cannot use this:
	//&& type.GetCustomAttribute<MetadataUpdateOriginalTypeAttribute>() is { OriginalType : not null } typeUpdate)
	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
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
