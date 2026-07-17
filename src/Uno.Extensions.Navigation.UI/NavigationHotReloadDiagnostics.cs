using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Uno.Extensions.Navigation.Regions;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(FrameworkElement), typeof(Uno.Extensions.Navigation.UI.NavigationHotReloadDiagnostics))]

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Diagnostic logging for navigation across hot-reload cycles (uno.extensions#3130).
/// Rides Uno's element-update pipeline (ElementUpdateAgent) and logs every callback —
/// including full region-tree dumps with view instance identities — relative to the HR
/// UI-update phase, so "was the live instance replaced, and what does navigation point
/// at" can be answered from the logs alone. All messages carry the [NAV-HR-DIAG] marker.
/// </summary>
internal static class NavigationHotReloadDiagnostics
{
	private const string Tag = "[NAV-HR-DIAG]";
	private const int MaxDumpDepth = 12;

	private static string Id(object? o)
		=> o is null ? "<null>" : $"{o.GetType().Name}#{RuntimeHelpers.GetHashCode(o):X8}";

	private static string FullId(object? o)
		=> o is null ? "<null>" : $"{o.GetType().FullName}#{RuntimeHelpers.GetHashCode(o):X8}";

	private static string TypeList(Type[]? types)
		=> types is null ? "<null>" : types.Length == 0 ? "<empty>" : string.Join(", ", types.Select(t => t.FullName ?? t.Name));

	public static void BeforeVisualTreeUpdate(Type[]? updatedTypes)
	{
		if (Region.Logger.IsEnabled(LogLevel.Warning))
		{
			Region.Logger.LogWarningMessage($"{Tag} BeforeVisualTreeUpdate types=[{TypeList(updatedTypes)}]");
			DumpRegionTrees("before-vt-update");
		}
	}

	public static void AfterVisualTreeUpdate(Type[]? updatedTypes)
	{
		if (Region.Logger.IsEnabled(LogLevel.Warning))
		{
			Region.Logger.LogWarningMessage($"{Tag} AfterVisualTreeUpdate types=[{TypeList(updatedTypes)}]");
			DumpRegionTrees("after-vt-update");
		}
	}

	public static void BeforeElementReplaced(FrameworkElement oldElement, FrameworkElement newElement, Type[]? updatedTypes)
	{
		if (Region.Logger.IsEnabled(LogLevel.Warning))
		{
			var parent = VisualTreeHelper.GetParent(oldElement);
			Region.Logger.LogWarningMessage(
				$"{Tag} BeforeElementReplaced old={FullId(oldElement)} new={FullId(newElement)} " +
				$"oldParent={Id(parent)} oldRegion={Id(oldElement.GetInstance())} oldName='{oldElement.GetName() ?? oldElement.Name}'");
		}
	}

	public static void AfterElementReplaced(FrameworkElement oldElement, FrameworkElement newElement, Type[]? updatedTypes)
	{
		if (Region.Logger.IsEnabled(LogLevel.Warning))
		{
			var newParent = VisualTreeHelper.GetParent(newElement);
			var frameInfo = string.Empty;
			if (newElement is Page newPage && newPage.Frame is { } frame)
			{
				frameInfo = $" frame={Id(frame)} frame.Content={Id(frame.Content)} frame.SourcePageType={frame.SourcePageType?.FullName} backStack={frame.BackStackDepth}";
			}

			Region.Logger.LogWarningMessage(
				$"{Tag} AfterElementReplaced old={FullId(oldElement)} new={FullId(newElement)} " +
				$"newParent={Id(newParent)} oldRegion={Id(oldElement.GetInstance())} newRegion={Id(newElement.GetInstance())} " +
				$"newName='{newElement.GetName() ?? newElement.Name}'{frameInfo}");
		}
	}

	public static void ReloadCompleted(Type[]? updatedTypes, bool uiUpdated)
	{
		if (Region.Logger.IsEnabled(LogLevel.Warning))
		{
			Region.Logger.LogWarningMessage($"{Tag} ReloadCompleted uiUpdated={uiUpdated} types=[{TypeList(updatedTypes)}]");
			DumpRegionTrees("reload-completed");
		}
	}

	/// <summary>
	/// Logs the full live region tree of every registered navigation context: region
	/// names, navigator types, active routes and the identity (type + hash) of every
	/// view instance — the ground truth of "what navigation points at" at this phase.
	/// </summary>
	internal static void DumpRegionTrees(string phase)
	{
		if (!Region.Logger.IsEnabled(LogLevel.Warning))
		{
			return;
		}

		try
		{
			var contextIndex = 0;
			foreach (var ctx in NavigationRouteUpdateHandler.ActiveContexts)
			{
				if (ctx.RootRegion is { } root)
				{
					Region.Logger.LogWarningMessage($"{Tag} region-dump phase={phase} ctx={contextIndex}");
					DumpRegion(root, depth: 0);
				}
				else
				{
					Region.Logger.LogWarningMessage($"{Tag} region-dump phase={phase} ctx={contextIndex}: no RootRegion");
				}

				contextIndex++;
			}
		}
		catch (Exception ex)
		{
			Region.Logger.LogWarningMessage($"{Tag} region-dump phase={phase} FAILED: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void DumpRegion(IRegion region, int depth)
	{
		if (depth > MaxDumpDepth)
		{
			Region.Logger.LogWarningMessage($"{Tag}   {new string(' ', depth * 2)}... depth limit reached");
			return;
		}

		var indent = new string(' ', depth * 2);
		var navigator = region.Navigator();
		var view = region.View;
		var viewInfo = view is null
			? "<null>"
			: $"{Id(view)} loaded={view.IsLoaded} vis={view.Visibility} opacity={view.Opacity}";

		Region.Logger.LogWarningMessage(
			$"{Tag}   {indent}region name='{region.Name ?? string.Empty}' navigator={Id(navigator)} " +
			$"route='{navigator?.Route?.ToString() ?? "<null>"}' view={viewInfo}");

		// The stale-instance question lives inside hosts: show what a Frame is actually
		// displaying, and which panel child a visibility navigator has visible.
		if (view is Frame frame)
		{
			Region.Logger.LogWarningMessage(
				$"{Tag}   {indent}  frame.Content={FullId(frame.Content)} sourcePageType={frame.SourcePageType?.FullName} backStack={frame.BackStackDepth}");
		}
		else if (view is Panel panel && Region.GetNavigator(panel) is { Length: > 0 } navigatorName)
		{
			foreach (var child in panel.Children.OfType<FrameworkElement>())
			{
				var childFrameInfo = string.Empty;
				if (child is Controls.FrameView fv && fv.Content is Frame navFrame)
				{
					childFrameInfo = $" frame.Content={FullId(navFrame.Content)}";
				}

				Region.Logger.LogWarningMessage(
					$"{Tag}   {indent}  panel({navigatorName}) child={Id(child)} name='{child.GetName() ?? child.Name}' vis={child.Visibility} opacity={child.Opacity}{childFrameInfo}");
			}
		}

		foreach (var child in region.Children.ToArray())
		{
			DumpRegion(child, depth + 1);
		}
	}
}
