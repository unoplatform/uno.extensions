using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Config;

/// <summary>
/// Set of configuration options for the MVUX framework.
/// </summary>
public class FeedConfiguration
{
	private static HotReloadSupport? _hotReload;
	/// <summary>
	/// Configures the hot-reload support for the MVUX framework.
	/// This 
	/// </summary>
	/// <remarks>This must be set before any feed or state is being created. Changing it later won't have any effect.</remarks>
	/// <remarks>The hot reload support for generated is enabled </remarks>
	/// <remarks>For full support of hot reload, you should consider to set the <see cref="DynamicReload"/> to <see cref="DynamicReloadSupport.All"/>.</remarks>
	internal static HotReloadSupport? HotReload
	{
		get => _hotReload;
		set
		{
			if (value is { } v && (v & HotReloadSupport.Disabled) != 0 && (v & HotReloadSupport.Enabled) != 0)
			{
				throw new ArgumentException($"The {nameof(HotReload)} value cannot be both {nameof(HotReloadSupport.Disabled)} and have any enabled flags.");
			}
			_hotReload = value;
		}
	}

	private static HotReloadSupport? _effectiveHotReload;

	/// <summary>
	/// Gets the current configuration of the hot-reload support for the MVUX framework.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static HotReloadSupport EffectiveHotReload => _effectiveHotReload ??= HotReload ?? HotReloadSupport.Disabled;

	/// <summary>
	/// Configures how a bindable should behave when a feed or state is removed from the model.
	/// </summary>
	/// <remarks>This usually applies only for bindable.</remarks>
	public static HotReloadRemovalBehavior HotReloadRemovalBehavior { get; set; } = HotReloadRemovalBehavior.KeepPrevious;

	/// <summary>
	/// Configures the dynamic reload support for the MVUX framework.
	/// </summary>
	/// <remarks>
	/// The "dynamic reload" feature refers to the ability to automatically refresh a feed based on an asynchronous operation that has awaited the value of another feed.
	/// For instance `MyFeed => Feed.Async(async ct => await MyOtherFeed)`, if the <see cref="DynamicReload"/> has the flag <see cref="DynamicReloadSupport.Async"/>,
	/// the `MyFeed` will be automatically refreshed when the `MyOtherFeed` feed is updated.
	/// </remarks>
	internal static DynamicReloadSupport DynamicReload { get; set; } = default; // Disabled by default for now
}
