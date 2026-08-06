using System.Reflection.Metadata;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.Regions;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(FrameworkElement), typeof(Uno.Extensions.Navigation.UI.NavigationFrameContentUpdateHandler))]

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Hot-reload handler that re-hooks navigation state after Uno's HR UI-update phase
/// replaced a frame's content outside navigation (uno.extensions#3130).
/// </summary>
/// <remarks>
/// Uno's element-update pipeline replaces live page instances in two ways: materialized
/// pages are swapped by the visual-tree walk (which raises <c>AfterElementReplaced</c>),
/// and never-materialized pages — which the walk cannot see — are patched directly on
/// <c>Frame.Content</c> by Uno's <c>FrameElementMetadataUpdateHandler</c>. Neither path
/// goes through navigation, so <see cref="FrameNavigator"/> keeps tracking the dead
/// instance. Running after the visual-tree phase, this handler walks the live region
/// trees and lets each <see cref="FrameNavigator"/> re-hook onto its frame's actual
/// content.
/// </remarks>
internal static class NavigationFrameContentUpdateHandler
{
	public static void AfterVisualTreeUpdate(Type[]? updatedTypes)
	{
		// Hot-Design-originated cycles arrive with an EMPTY type list (the write is muted
		// upstream) — nothing was replaced, so there is nothing to re-hook. A null list
		// means "types unknown" and must still re-hook: the mismatch check below is what
		// decides whether anything actually changed.
		if (updatedTypes is { Length: 0 })
		{
			return;
		}

		foreach (var root in NavigationRouteUpdateHandler.ActiveContexts
			.Select(static ctx => ctx.RootRegion)
			.OfType<IRegion>())
		{
			RehookReplacedFrameContent(root, updatedTypes);
		}
	}

	private static void RehookReplacedFrameContent(IRegion region, Type[]? updatedTypes)
	{
		if (region.Navigator() is FrameNavigator frameNavigator)
		{
			frameNavigator.RehookCurrentViewAfterHotReload(updatedTypes);
		}

		foreach (var child in region.Children.ToArray())
		{
			RehookReplacedFrameContent(child, updatedTypes);
		}
	}
}
