using System.Reflection.Metadata;
using Uno.Extensions.Navigation.Regions;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(FrameworkElement), typeof(Uno.Extensions.Navigation.UI.NavigationVisibilityUpdateHandler))]

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Hot-reload handler that preserves <see cref="UIElement.Visibility"/> across
/// XAML hot-reload updates, scoped to elements managed by the Uno.Extensions
/// navigation engine.
/// </summary>
/// <remarks>
/// When a type is hot-reloaded, <c>ReplaceViewInstance</c> creates a new instance
/// via <c>Activator.CreateInstance</c>, which calls <c>InitializeComponent()</c>.
/// This recreates the entire visual subtree from XAML, resetting all child
/// DependencyProperty values to their XAML defaults. Navigation panels whose
/// Visibility was changed at runtime (e.g. by <c>PanelVisiblityNavigator</c>)
/// lose their state.
///
/// Only elements participating in Uno.Extensions region-based navigation —
/// identified by having <c>Region.Name</c> set or being a direct child of a
/// panel with <c>Region.Navigator</c> — need Visibility preservation.
/// </remarks>
internal static class NavigationVisibilityUpdateHandler
{
	private const string VisibilityKey = "Visibility";
	private const string HadActiveNavigationKey = "HadActiveNavigation";

	public static void CaptureState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[]? updatedTypes)
	{
		if (IsNavigationManagedElement(element))
		{
			stateDictionary[VisibilityKey] = element.Visibility;
		}

		// Capture whether this element (or any child) had a region with active navigation.
		// After HR creates a new element instance, the new NavigationRegion objects won't
		// have _wasUnloaded set, so HandleLoaded won't re-cascade the parent route.
		// We mark such elements so RestoreState can flag the new regions for re-cascade.
		if (element.GetInstance() is NavigationRegion { Parent: not null } region)
		{
			var navigator = region.Navigator();
			if (navigator?.Route is { Base.Length: > 0 } || region.Parent.Navigator()?.Route is { Base.Length: > 0 })
			{
				stateDictionary[HadActiveNavigationKey] = true;
			}
		}
	}

	public static Task RestoreState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[]? updatedTypes)
	{
		if (stateDictionary.TryGetValue(VisibilityKey, out var value) && value is Visibility savedVisibility)
		{
			if (element.Visibility != savedVisibility)
			{
				element.Visibility = savedVisibility;
			}
		}

		// If the old element had active navigation, mark the new region so
		// HandleLoaded's re-cascade path (which checks _replacedByHotReload)
		// can run on any subsequent reload of this region.
		//
		// In practice the new element's Loaded event has already fired by the
		// time RestoreState reaches it (Uno's HR processor swaps the view
		// before invoking RestoreState), so the flag set here is purely
		// defensive — the actual cascade for this swap is kicked off via
		// NavigationRouteUpdateHandler.ScheduleCascadeForAllContexts below,
		// which walks the live region tree and dispatches any newly-needed
		// IsDefault nested route onto the matching child region.
		if (stateDictionary.ContainsKey(HadActiveNavigationKey)
			&& element.GetInstance() is NavigationRegion newRegion)
		{
			newRegion.MarkReplacedByHotReload();

			// Only trigger a route cascade when at least one updated type is a
			// navigation-registered view or view-model.  A XAML-only HR cycle
			// (e.g. a Text or colour change) replaces the element instance but
			// does not add new routes; cascading here re-mounts the page and
			// lets x:Uid resolution overwrite property edits that Hot Design
			// just wrote, making them appear to revert.  See studio.live#2293.
			NavigationRouteUpdateHandler.ScheduleCascadeForAllContextsIfRouteRelevant(updatedTypes);
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Determines whether the element's Visibility is managed by the
	/// Uno.Extensions navigation engine and should be preserved across hot-reload.
	/// </summary>
	private static bool IsNavigationManagedElement(FrameworkElement element)
	{
		// Check 1: element has Region.Name set — it's a named navigation target
		if (Region.GetName(element) is { Length: > 0 })
		{
			return true;
		}

		// Check 2: element's parent panel has Region.Navigator set —
		// element is a child managed by a visibility navigator
		if (element.Parent is Panel parent && Region.GetNavigator(parent) is { Length: > 0 })
		{
			return true;
		}

		return false;
	}
}
