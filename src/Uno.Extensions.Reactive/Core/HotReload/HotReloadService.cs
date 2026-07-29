using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HotReloadService
{
	private static readonly ILogger _log = typeof(HotReloadService).CreateLog();
	private static readonly bool _trace = _log.IsEnabled(LogLevel.Trace);

	internal static event Action<Type[]>? ApplicationUpdated;

	// Maps an original model type to its latest hot-reloaded "shadow" type generation, so that
	// a model instance constructed AFTER a hot-reload (where `new TModel()` would otherwise
	// pick up the original type's stale lambdas / cached delegates) can be redirected to the
	// freshest generation. Populated from `UpdateApplication` for every type that carries
	// `MetadataUpdateOriginalTypeAttribute`.
	//
	// Both key and value are strong `Type` references. The map is only ever added to, so on its own
	// it would keep every hot-reloaded model type — and therefore that type's (collectible)
	// AssemblyLoadContext — alive for the process lifetime. A downstream host that loads previewed
	// apps into their own collectible ALCs would leak the entire type graph of every app it ever
	// previewed. `ClearCache`/`Reset` and the per-ALC unload sweep below release those entries.
	private static readonly ConcurrentDictionary<Type, Type> _latestShadow = new();

	// Collectible ALCs we have already subscribed to, so a single unload handler is attached per ALC.
	private static readonly ConcurrentDictionary<AssemblyLoadContext, byte> _watchedContexts = new();

	/// <summary>
	/// Removes every tracked shadow mapping. Intended to be called when the host that owns the
	/// previewed app is torn down, so no app type is retained past its lifetime.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void Reset()
	{
		_latestShadow.Clear();
		_watchedContexts.Clear();
		if (_trace) _log.Trace("Cleared all MVUX hot-patch shadow mappings.");
	}

	// Number of tracked shadow mappings. Exposed to tests so they can observe release without
	// reaching into the private field.
	internal static int TrackedShadowCount => _latestShadow.Count;

	// Registers a shadow mapping exactly as `UpdateApplication` does (map entry + collectible-ALC
	// unload watch), without requiring the runtime metadata-update attributes. Test seam only.
	internal static void TrackShadowForTest(Type originalType, Type shadowType)
	{
		_latestShadow[originalType] = shadowType;
		WatchForUnload(originalType);
		WatchForUnload(shadowType);
	}

	// Invoked by the runtime's metadata-update pipeline. Drop the shadow mappings for the supplied
	// types (both as original keys and as shadow values) so stale generations are not retained.
	internal static void ClearCache(Type[]? types)
	{
		if (types is null or { Length: 0 })
		{
			return;
		}

		var targets = new HashSet<Type>(types);
		foreach (var entry in _latestShadow)
		{
			if (targets.Contains(entry.Key) || targets.Contains(entry.Value))
			{
				_latestShadow.TryRemove(entry.Key, out _);
			}
		}
	}

	// Ensure the shadow entries keyed/valued by types from a collectible ALC are dropped when that
	// ALC unloads, so the map never pins a previewed app's ALC.
	private static void WatchForUnload(Type type)
	{
		if (AssemblyLoadContext.GetLoadContext(type.Assembly) is not { IsCollectible: true } context)
		{
			return;
		}

		if (_watchedContexts.TryAdd(context, 0))
		{
			context.Unloading += OnContextUnloading;
		}
	}

	private static void OnContextUnloading(AssemblyLoadContext context)
	{
		foreach (var entry in _latestShadow)
		{
			if (AssemblyLoadContext.GetLoadContext(entry.Key.Assembly) == context ||
				AssemblyLoadContext.GetLoadContext(entry.Value.Assembly) == context)
			{
				_latestShadow.TryRemove(entry.Key, out _);
			}
		}

		_watchedContexts.TryRemove(context, out _);
		context.Unloading -= OnContextUnloading;
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

			// Track the latest shadow generation so that ANY new bindable instance constructed
			// after this update can self-patch its (otherwise stale-lambda-bound) model to the
			// freshest type. Cf. `TryHotPatch`.
			_latestShadow[originalType] = type;

			// If either type lives in a collectible ALC, make sure the mapping is dropped when that
			// ALC unloads, so we never keep a previewed app's ALC alive.
			WatchForUnload(originalType);
			WatchForUnload(type);

			if (_log.IsEnabled(LogLevel.Information)) _log.Info($"Hot-patching bindables of {originalType} to use the updated {type}.");

			BindableViewModelBase.HotPatch(model.Bindable, originalType, type);
		}

		ApplicationUpdated?.Invoke(types);
	}

	/// <summary>
	/// Redirects a freshly-constructed bindable to the latest hot-reloaded shadow generation of its model,
	/// if a delta has been applied since startup. Called from <see cref="BindableViewModelBase"/> at the
	/// end of HR initialization.
	/// </summary>
	/// <remarks>
	/// Without this, a `new TBindable()` issued AFTER an HR delta would construct a `new TModel()`
	/// of the original type — whose lambdas (e.g. those captured by `Feed.Async`) still resolve to the
	/// pre-update IL. By piggy-backing on the same per-state hot-swap path used for normal HR deltas,
	/// this fresh bindable observes the updated values.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
	public static void TryHotPatch(BindableViewModelBase target)
	{
		if (_latestShadow.IsEmpty)
		{
			return;
		}

		if (target.GetType().GetCustomAttribute<Bindings.BindableAttribute>() is not { Model: { } modelType })
		{
			return;
		}

		// `BindableAttribute.Model` may itself be remapped by EnC to a shadow type when the bindable
		// is constructed after a hot-reload — resolve back to the canonical original.
		var canonicalOriginal = GetOriginalType(modelType) ?? modelType;

		// Walk base types: if only an ancestor was edited, patch with the ancestor shadow so
		// inherited feeds get hot-swapped. Derived-only feeds are kept intact by the patcher.
		Type? shadowType = null;
		for (var t = canonicalOriginal; t is not null && t != typeof(object); t = t.BaseType)
		{
			if (_latestShadow.TryGetValue(t, out shadowType))
			{
				canonicalOriginal = t;
				break;
			}
		}

		if (shadowType is null)
		{
			// No HR delta has fired for this model or any of its ancestors since startup.
			return;
		}

		// Note: even when `shadowType == modelType` we still patch — `new TModel()` invoked from the
		// generated bindable ctor produces an instance of the ORIGINAL type (its IL has the original
		// ctor token), whose lambdas resolve to pre-HR delegates. Only re-creating the model via the
		// shadow type's metadata gives us the post-HR lambdas.
		try
		{
			target.HotPatch(canonicalOriginal, shadowType);
		}
		catch (Exception e)
		{
			if (_log.IsEnabled(LogLevel.Error)) _log.Error(e, $"Failed to self-hot-patch a freshly-constructed bindable of {canonicalOriginal}.");
		}
	}

	// As the MetadataUpdateOriginalTypeAttribute might have been generated in the project, we have to use reflection instead of cannot use this:
	//&& type.GetCustomAttribute<MetadataUpdateOriginalTypeAttribute>() is { OriginalType : not null } typeUpdate)
	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
	internal static Type? GetOriginalType(Type type)
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
