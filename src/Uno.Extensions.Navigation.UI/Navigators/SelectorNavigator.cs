using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Navigation.Navigators;

public abstract class SelectorNavigator<TControl> : ControlNavigator<TControl>
	where TControl : class
{
	private Action? _detachSelectionChanged;

	public override void ControlInitialize()
	{
		if (Control is not null)
		{
			_detachSelectionChanged = AttachSelectionChanged((sender, selected) => _ = SelectionChanged(sender, selected));
		}
		else
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Control is null, so unable to attach selection changed handler");
			}
		}
	}

	protected abstract FrameworkElement? SelectedItem { get; set; }

	protected abstract Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged);

	protected abstract IEnumerable<FrameworkElement> Items { get; }

	protected override FrameworkElement? CurrentView => SelectedItem;

	// ═══════════════════════════════════════════════════════════════════════
	// TEST 11 INVESTIGATION NOTES (When_TabBarItemAddedViaXamlHR_WithoutRouteRegistration)
	// ═══════════════════════════════════════════════════════════════════════
	//
	// GOAL: A TabBarItem added via XAML Hot Reload (no C# route registration)
	//       should be navigable. The framework should lazily create a route.
	//
	// CURRENT STATUS: FAILS — PanelVisiblityNavigator.Show("TabThree") is
	//                 never called. No FrameView for TabThree is ever created.
	//                 After 30s the test times out with:
	//                 "ContentGrid children: [FrameView[Region.Name='TabOne']]"
	//
	// ─── NAVIGATION FLOW TRACE ─────────────────────────────────────────────
	//
	// 1. tabBarNavigator.NavigateRouteAsync("TabThree")
	//    → Navigator.NavigateAsync → RedirectNavigateAsync
	//
	// 2. RedirectNavigateAsync (TabBarNavigator):
	//    rm = Resolver.FindByPath("TabThree") → NULL (not registered yet)
	//    → RedirectForImplicitForwardNavigation SKIPPED (rm is null)
	//    → CanNavigate(route) called:
	//        → RegionCanNavigate (SelectorNavigator override below)
	//        → EnsureRouteRegistered("TabThree") INSERTS route here
	//        → returns true
	//    → ParentCanNavigate(route) called:
	//        → Parent is Composite (unnamed Grid region)
	//        → Composite.CanNavigate → Composite.RegionCanNavigate
	//        → Composite iterates ALL children (PanelVisiblityNav + TabBarNav)
	//        → PanelVisiblityNavigator.CanNavigate("TabThree"):
	//            → routeMap = Resolver.FindByPath("TabThree") → NOW FOUND
	//              (because EnsureRouteRegistered ran above)
	//            → base.RegionCanNavigate: Route?.IsEmpty() is true for
	//              PanelVisiblityNav (its Route is Route.Empty after TabOne nav),
	//              so parent-comparison check is SKIPPED → returns true
	//            → PanelVisiblityNavigator.RegionCanNavigate checks:
	//              routeMap.RenderView (= siblingRoute.View which is
	//              HotReloadTabContentPage, a Page subclass, so
	//              IsSubclassOf(FrameworkElement) = true) → returns true
	//        → TabBarNavigator.CanNavigate → returns true (has item)
	//        → ALL children return true → Composite returns true
	//    → ParentCanNavigate = true
	//    → CanNavigate(true) && !ParentCanNavigate(true) = FALSE
	//    → Falls through to Region.Parent.NavigateAsync(request)
	//      i.e. request bubbles UP to Composite
	//
	// 3. Composite.NavigateAsync → RedirectNavigateAsync (Composite):
	//    rm = Resolver.FindByPath("TabThree") → FOUND (inserted in step 2)
	//    → RedirectForImplicitForwardNavigation:
	//      Iterates ancestors. Finds the FrameNavigator ancestor.
	//      ancestorMap = Resolver.FindByPath(FrameNavigator.Route.Base)
	//                  = FindByPath("HotReloadTabBarXamlPage") → FOUND
	//      ancestorMap.Parent = root route ""
	//      rm.Parent = "HotReloadTabBarXamlPage" route
	//      ancestorMap.Parent != rm.Parent → SKIP (no match)
	//      → No ancestor matches → returns default
	//    → CanNavigate && !ParentCanNavigate:
	//      CanNavigate = true (composite, all children can navigate)
	//      ParentCanNavigate: parent is FrameNavigator
	//        → FrameNavigator is NOT composite/PanelVisibility/Selector
	//        → returns false
	//      → true && !false = true → handles locally (no redirection)
	//
	// 4. Composite.RegionNavigateAsync → CoreNavigateAsync:
	//    → EnsureChildRegionsAreLoaded
	//    → Filters children: region.Name == request.Route.Base ("TabThree")
	//      OR region.Name == Route?.Base (composite's Route.Base)
	//      OR string.IsNullOrWhiteSpace(region.Name)
	//    → Children are: PanelVisiblityNav region (unnamed → MATCHES)
	//                     TabBarNav region (unnamed → MATCHES)
	//    → NavigateChildRegions forwards to both children
	//
	// 5a. TabBarNavigator receives request:
	//    → RedirectNavigateAsync: request.Route.IsInternal = true → returns default
	//    → RegionNavigateAsync → ControlCoreNavigateAsync:
	//      routeMap found, RegionCanNavigate = true
	//      → Show("TabThree") → selects TabThree item ✓
	//      → returns Route.Empty (executedPath is null from Show)
	//    → BUT executedPath == null means ExecuteRequestAsync returns Route.Empty
	//      WITHOUT calling InitializeCurrentView. This is fine for TabBar
	//      (it only selects, doesn't create content).
	//
	// 5b. PanelVisiblityNavigator receives request:
	//    → RedirectNavigateAsync: request.Route.IsInternal = true → returns default
	//    → RegionNavigateAsync → ControlCoreNavigateAsync:
	//      routeMap = Resolver.FindByPath("TabThree") → FOUND
	//      RegionCanNavigate(route, routeMap):
	//        → base.RegionCanNavigate: Route?.IsEmpty() == true → skip parent check → true
	//        → routeMap.RenderView = HotReloadTabContentPage (Page subclass)
	//          → IsSubclassOf(typeof(FrameworkElement)) → TRUE → returns true
	//      → ControlNavigateAsync → ExecuteRequestAsync:
	//        mapping = Resolver.FindByPath("TabThree")
	//        mapping.RenderView = HotReloadTabContentPage (a Page)
	//        → Show("TabThree", typeof(HotReloadTabContentPage), data)
	//
	// 6. PanelVisiblityNavigator.Show("TabThree", typeof(HotReloadTabContentPage)):
	//    → FindByPath("TabThree") → null (no visual child yet)
	//    → viewType is HotReloadTabContentPage, which IS SubclassOf(Page)
	//      → so the override: if (viewType.IsSubclassOf(typeof(Page)))
	//        viewType = typeof(FrameView)
	//    → Creates FrameView, sets Region.Name = "TabThree"
	//    → Adds to Panel → returns null (because controlToShow is FrameView)
	//
	// 7. Back in ExecuteRequestAsync (ControlNavigator.cs line 50):
	//    executedPath = null (Show returned null for FrameView)
	//    → returns Route.Empty WITHOUT calling InitializeCurrentView
	//    → The FrameView is added but NEVER navigated into!
	//    → No Frame content is set, no HotReloadTabContentPage is created
	//
	// ─── ROOT CAUSE ─────────────────────────────────────────────────────────
	//
	// The FrameView IS created and added to ContentGrid, but it's never
	// initialized/navigated. When Show() returns null (for FrameView),
	// ControlNavigator.ExecuteRequestAsync returns Route.Empty and skips
	// InitializeCurrentView. This means the FrameView's inner FrameNavigator
	// never receives the navigation request.
	//
	// For normal (pre-registered) tab routes, this works because:
	//   - ControlNavigator.CoreNavigateAsync calls ExecuteRequestAsync THEN
	//     base.CoreNavigateAsync (Navigator.CoreNavigateAsync)
	//   - base.CoreNavigateAsync forwards the request to child regions
	//   - The FrameView's region becomes a child, and the request propagates
	//
	// BUT the issue may be that:
	//   (a) The trimmed request after ExecuteRequestAsync returns Route.Empty
	//       becomes empty, so base.CoreNavigateAsync has nothing to forward, OR
	//   (b) The FrameView's region hasn't attached as a child yet by the time
	//       base.CoreNavigateAsync runs (timing/async issue), OR
	//   (c) The request trimming (request.Route.Trim(regionResponse.Route))
	//       strips "TabThree" from the route, leaving nothing for children.
	//
	// LIKELY (a)+(c): ExecuteRequestAsync returns Route.Empty which has
	// Base="" (empty). The Trim operation:
	//   request.Route.Trim(Route.Empty) — Route.Empty has empty Base,
	//   so while loop doesn't strip anything. BUT executedRoute from
	//   ExecuteRequestAsync has Base="" Path=null, and the Trim compares
	//   route.Base == handledRoute.Base — both are "" for Route.Empty,
	//   but IsNullOrWhiteSpace check prevents stripping.
	//   Actually the regionResponse.Route would be the Route.Empty from
	//   ExecuteRequestAsync, and we trim request.Route by it.
	//   request.Route is "TabThree/..." route. Trim(Route.Empty) should
	//   leave it untouched. So the route SHOULD still say "TabThree" when
	//   forwarded to children.
	//
	// NEXT STEPS TO INVESTIGATE:
	//   1. Verify whether the FrameView's region attaches as a child of
	//      PanelVisiblityNavigator's region BEFORE base.CoreNavigateAsync
	//      runs. If EnsureChildRegionsAreLoaded doesn't wait for the
	//      newly-added FrameView, the child region won't exist yet.
	//   2. Check if ControlNavigator.CoreNavigateAsync line 162
	//      (base.CoreNavigateAsync) filters children correctly — the
	//      FrameView's region.Name would be "TabThree" and request.Route.Base
	//      is "TabThree", so it SHOULD match the child filter.
	//   3. Consider whether the fix should be in ControlNavigator.ExecuteRequestAsync
	//      to NOT return Route.Empty when Show returns null (FrameView case)
	//      but instead return the original route so InitializeCurrentView runs.
	//      Or alternatively, in PanelVisiblityNavigator.Show, return the path
	//      instead of null for FrameView, letting the normal init flow proceed.
	//   4. Alternative approach: instead of lazy route insertion here, pre-register
	//      routes when TabBar items change (e.g. via LayoutUpdated or
	//      ItemsChanged callback in ControlInitialize).
	//
	// ═══════════════════════════════════════════════════════════════════════

	protected override async Task<bool> RegionCanNavigate(Route route, RouteInfo? routeMap)
	{
		// When a tab item exists but has no registered route (e.g. added via XAML HR),
		// lazily insert a route with a FrameView so that the sibling
		// PanelVisiblityNavigator also accepts the navigation and the request
		// propagates through the composite parent to all children.
		if (routeMap is null && !string.IsNullOrWhiteSpace(route.Base))
		{
			var hasItem = await Dispatcher.ExecuteAsync(async cancellation =>
			{
				return FindByPath(route.Base) is not null;
			});

			if (hasItem)
			{
				EnsureRouteRegistered(route.Base);
				routeMap = Resolver.FindByPath(route.Base);
			}
		}

		if (!await base.RegionCanNavigate(route, routeMap))
		{
			return false;
		}

		return await Dispatcher.ExecuteAsync(async cancellation =>
		{
			return FindByPath(routeMap?.Path ?? route.Base) is not null;
		});
	}

	/// <summary>
	/// Inserts a route for the given path if one does not already exist.
	/// The new route is created as a sibling of existing tab routes (same Parent)
	/// and inherits the View and ViewModel from a sibling so that the
	/// <see cref="PanelVisiblityNavigator"/> can create a FrameView and the
	/// inner FrameNavigator can navigate to the correct page type.
	/// </summary>
	private void EnsureRouteRegistered(string path)
	{
		if (Resolver.FindByPath(path) is not null)
		{
			return;
		}

		RouteInfo? siblingRoute = null;
		foreach (var item in Items)
		{
			var itemPath = item.GetRegionOrElementName()?.WithoutQualifier();
			if (string.IsNullOrEmpty(itemPath))
			{
				continue;
			}

			var existing = Resolver.FindByPath(itemPath);
			if (existing is not null)
			{
				siblingRoute = existing;
				break;
			}
		}

		var newRoute = new RouteInfo(
			path,
			View: siblingRoute?.View,
			ViewModel: siblingRoute?.ViewModel)
		{
			Parent = siblingRoute?.Parent
		};
		Resolver.InsertRoute(newRoute);
	}

	protected SelectorNavigator(
		ILogger logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider.RegionControl as TControl)
	{
	}

	protected override async Task<string?> Show(
		string? path,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type? viewType,
		object? data)
	{
		if (Control is null)
		{
			return null;
		}

		// Invoke detach and clean up reference to the delegate
		var detach = _detachSelectionChanged;
		_detachSelectionChanged = null;
		detach?.Invoke();
		try
		{
			var item = FindByPath(path);

			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Item to select found ({item is not null})");
			}

			// Only set the selected item if it's changed (and not null)
			// to prevent any visual artefacts that may result from setting
			// the same item multiple times
			if (item != null &&
				SelectedItem != item)
			{
				SelectedItem = item;
			}

			// Don't return path, as we need for path to be passed down to children
			return default;
		}
		finally
		{
			_detachSelectionChanged = AttachSelectionChanged((sender, selected) => _ = SelectionChanged(sender, selected));
		}
	}

	protected async Task SelectionChanged(FrameworkElement sender, FrameworkElement? selectedItem)
	{
		if (selectedItem is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Selected Item is null");
			}

			return;
		}

		var path = selectedItem.GetRegionOrElementName();

		if (Logger.IsEnabled(LogLevel.Trace))
		{
			Logger.LogTraceMessage($"Selected region name is {path}");
		}


		var nav = Region.Navigator();

		if (path is null ||
			string.IsNullOrEmpty(path) ||
			nav is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Path is {path} and Navigator is {(nav is null ? "null" : "not null")}");
			}

			return;
		}

		var data = selectedItem.GetData();

		await nav.NavigateRouteAsync(sender, path, data: data);
	}


	private FrameworkElement? FindByPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path) || Control is null)
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"Attempting to find empty path ({path}) or Control is null ({Control is null})");
			}

			return default;
		}

		var item = (from mi in Items
					where (mi.GetRegionOrElementName().WithoutQualifier() == path)
					select mi).FirstOrDefault();
		return item;
	}
}
