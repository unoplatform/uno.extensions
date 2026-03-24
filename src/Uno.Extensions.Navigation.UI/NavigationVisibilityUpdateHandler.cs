using System.Reflection.Metadata;

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

	public static void CaptureState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[]? updatedTypes)
	{
		if (IsNavigationManagedElement(element))
		{
			stateDictionary[VisibilityKey] = element.Visibility;
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
