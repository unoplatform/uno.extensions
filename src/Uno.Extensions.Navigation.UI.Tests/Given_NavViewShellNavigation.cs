using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;
using Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Reproduces the "cannot navigate back to the default tab" report: an app whose shell is a
/// NavigationView + visibility content region starts on the IsDefault route ("NavHome"); selecting
/// another item works, but selecting the default item again must switch content back to it.
///
/// Two layout variants are covered:
///  - <see cref="NavViewShellPage"/>: content grid nested INSIDE NavigationView.Content
///    (the layout emitted by app scaffolders / docs).
///  - <see cref="TabbedMainPage"/>: content grid as a SIBLING of the NavigationView
///    (the layout used by Given_TabNavigation).
///
/// Navigation is driven by setting NavigationView.SelectedItem — the same code path a user
/// click takes (SelectionChanged → SelectorNavigator.SelectionChanged → NavigateRouteAsync),
/// unlike Given_TabNavigation which drives the tab FrameNavigators programmatically.
///
/// Route structure (mirrors a scaffolded app's RegisterRoutes):
///   "NavMain" (NavViewShellPage) [IsDefault]
///     "NavHome" (NavHomePage) [IsDefault]
///     "NavMenu" (NavMenuPage)
///     "NavOrders" (NavOrdersPage)
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NavViewShellNavigation
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private async Task<(IHost Host, ContentControl Root)> SetupNestedShellAsync()
	{
		var window = new Window();

		var host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_NavViewShellNavigation).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<NavViewShellPage>(),
								new ViewMap<NavHomePage>(),
								new ViewMap<NavMenuPage>(),
								new ViewMap<NavOrdersPage>());

							routes.Register(
								new RouteMap("NavMain", View: views.FindByView<NavViewShellPage>(), IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("NavHome", View: views.FindByView<NavHomePage>(), IsDefault: true),
										new RouteMap("NavMenu", View: views.FindByView<NavMenuPage>()),
										new RouteMap("NavOrders", View: views.FindByView<NavOrdersPage>()),
									}));
						})
					.Build();
				return h;
			},
			initialRoute: "NavMain");

		var root = (ContentControl)window.Content!;
		return (host, root);
	}

	private static TPage? ResolveShellPage<TPage>(ContentControl root)
		where TPage : Page
	{
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is TPage page)
		{
			return page;
		}

		return root.Content as TPage;
	}

	private static FrameView? FindTabFrameView(Panel contentGrid, string tabName)
		=> contentGrid.Children
			.OfType<FrameView>()
			.FirstOrDefault(fv => Region.GetName(fv) == tabName || fv.Name == tabName);

	private static bool IsTabVisible(Panel contentGrid, string tabName)
		=> FindTabFrameView(contentGrid, tabName) is { Visibility: Visibility.Visible };

	/// <summary>
	/// True once the panel has settled on <paramref name="visibleTab"/>: its FrameView is
	/// visible and every other FrameView in the panel is collapsed. Show() makes the incoming
	/// view visible immediately while PostNavigateAsync collapses the others only after the
	/// child cascade completes, so checking a single tab's visibility mid-flight races.
	/// </summary>
	private static bool IsOnlyVisibleTab(Panel contentGrid, string visibleTab)
	{
		var frameViews = contentGrid.Children.OfType<FrameView>().ToArray();
		if (frameViews.Length == 0)
		{
			return false;
		}

		var target = frameViews.FirstOrDefault(fv => Region.GetName(fv) == visibleTab || fv.Name == visibleTab);
		if (target is not { Visibility: Visibility.Visible })
		{
			return false;
		}

		return frameViews.All(fv => ReferenceEquals(fv, target) || fv.Visibility == Visibility.Collapsed);
	}

	/// <summary>
	/// Nested layout (scaffolded shell): select another tab via the NavigationView, then select
	/// the default tab again — the content region must switch back to the default tab's view.
	/// </summary>
	[TestMethod]
	public async Task When_NestedShell_SelectOtherThenDefault_Then_DefaultContentShown(CancellationToken ct)
	{
		var (host, root) = await SetupNestedShellAsync();

		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(Timeout);

			NavViewShellPage? shell = null;
			await UIHelper.WaitFor(() =>
			{
				shell = ResolveShellPage<NavViewShellPage>(root);
				return shell is not null;
			}, cts.Token);

			// Default route: NavHome content shown.
			await UIHelper.WaitFor(() => IsTabVisible(shell!.ContentGrid, "NavHome"), cts.Token);

			// Act 1: user selects "Menu" in the NavigationView.
			shell!.NavView.SelectedItem = shell.MenuItem;

			await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavMenu"), cts.Token);

			// Act 2: user selects "Home" (the IsDefault route) again.
			shell.NavView.SelectedItem = shell.HomeItem;

			// Assert: Home content must come back (and Menu must collapse).
			using var backCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			backCts.CancelAfter(Timeout);
			try
			{
				await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavHome"), backCts.Token);
			}
			catch (OperationCanceledException)
			{
				// Fall through to the assertion below for a readable failure message.
			}

			IsTabVisible(shell.ContentGrid, "NavHome").Should().BeTrue(
				"selecting the default tab again must switch the content region back to it");
			IsTabVisible(shell.ContentGrid, "NavMenu").Should().BeFalse(
				"the Menu tab content should be collapsed after navigating back to Home");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Nested layout: cycle through every tab and return to the default one, mirroring the
	/// user gesture trail from the bug report (Menu → Orders → Menu → Home).
	/// </summary>
	[TestMethod]
	public async Task When_NestedShell_CycleAllTabsThenDefault_Then_DefaultContentShown(CancellationToken ct)
	{
		var (host, root) = await SetupNestedShellAsync();

		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(TimeSpan.FromSeconds(30));

			NavViewShellPage? shell = null;
			await UIHelper.WaitFor(() =>
			{
				shell = ResolveShellPage<NavViewShellPage>(root);
				return shell is not null;
			}, cts.Token);

			await UIHelper.WaitFor(() => IsTabVisible(shell!.ContentGrid, "NavHome"), cts.Token);

			shell!.NavView.SelectedItem = shell.MenuItem;
			await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavMenu"), cts.Token);

			shell.NavView.SelectedItem = shell.OrdersItem;
			await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavOrders"), cts.Token);

			shell.NavView.SelectedItem = shell.MenuItem;
			await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavMenu"), cts.Token);

			shell.NavView.SelectedItem = shell.HomeItem;

			using var backCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			backCts.CancelAfter(Timeout);
			try
			{
				await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavHome"), backCts.Token);
			}
			catch (OperationCanceledException)
			{
			}

			IsOnlyVisibleTab(shell.ContentGrid, "NavHome").Should().BeTrue(
				"after cycling tabs, selecting the default tab must show its content again");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Reproduces the hosted-preview failure (spec 006 seam): the hosting tree is re-grafted
	/// (detached and re-attached) while the initial navigation is still cascading into the
	/// default tab's FrameView. After the re-graft the app must fully recover: the default
	/// tab shows, navigating away works, and — the reported bug — navigating BACK to the
	/// default tab must still work.
	/// </summary>
	[TestMethod]
	public async Task When_TreeRegraftDuringStartup_Then_DefaultTabStillReachable(CancellationToken ct)
	{
		var window = new Window();

		var initTask = window.InitializeNavigationAsync(
			buildHost: () => Task.FromResult(UnoHost
				.CreateDefaultBuilder(typeof(Given_NavViewShellNavigation).Assembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<NavViewShellPage>(),
							new ViewMap<NavHomePage>(),
							new ViewMap<NavMenuPage>(),
							new ViewMap<NavOrdersPage>());

						routes.Register(
							new RouteMap("NavMain", View: views.FindByView<NavViewShellPage>(), IsDefault: true,
								Nested: new RouteMap[]
								{
									new RouteMap("NavHome", View: views.FindByView<NavHomePage>(), IsDefault: true),
									new RouteMap("NavMenu", View: views.FindByView<NavMenuPage>()),
									new RouteMap("NavOrders", View: views.FindByView<NavOrdersPage>()),
								}));
					})
				.Build()),
			initialRoute: "NavMain");

		var root = window.Content as ContentControl;
		root.Should().NotBeNull("Window.Content should be the navigation root ContentControl");

		IHost? host = null;
		var phase = "setup";
		try
		{
			// Find the default tab's FrameView inner Frame as soon as it is created and hook
			// its Loaded before it fires — the deterministic detach seam from spec 006.
			NavViewShellPage? shell = null;
			Frame? homeFrame = null;
			var detachedDuringLoad = false;

			void OnHomeFrameLoaded(object s, RoutedEventArgs e)
			{
				homeFrame!.Loaded -= OnHomeFrameLoaded;

				// Re-graft equivalent: the host detaches the app content mid-bootstrap
				// (production: the preview host re-hosting an ALC-loaded app's content).
				detachedDuringLoad = true;
				window.Content = new Border();
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() =>
				{
					shell ??= ResolveShellPage<NavViewShellPage>(root!);
					if (shell is not null && homeFrame is null &&
						FindTabFrameView(shell.ContentGrid, "NavHome")?.Content is Frame f)
					{
						homeFrame = f;
						if (!f.IsLoaded)
						{
							f.Loaded += OnHomeFrameLoaded;
						}
						else
						{
							// Already loaded — detach immediately to exercise the same re-graft.
							detachedDuringLoad = true;
							window.Content = new Border();
						}
					}

					return homeFrame is not null;
				}, cts.Token);
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() => detachedDuringLoad, cts.Token);
			}

			detachedDuringLoad.Should().BeTrue("the detach must observe the load (scenario precondition)");

			// The initial navigation must complete even though the content is detached
			// (spec 006 guarantees this for the root-frame seam). Track — but don't await
			// unguarded — so a mid-cascade hang surfaces as an assertion, not an engine timeout.
			phase = "await-init-detached";
			var initCompleted = await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(15), ct)) == initTask;
			if (initCompleted)
			{
				host = await initTask;
			}

			// Re-graft completes: the app content returns to the tree.
			phase = "re-attach";
			window.Content = root;

			if (!initCompleted)
			{
				// Give the resumed tree a chance to unblock the in-flight navigation.
				phase = "await-init-reattached";
				initCompleted = await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(15), ct)) == initTask;
				if (initCompleted)
				{
					host = await initTask;
				}
			}

			// Recovery: the default tab must (eventually) be shown.
			phase = "wait-home-initial";
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				try
				{
					await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell!.ContentGrid, "NavHome"), cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
			}

			IsOnlyVisibleTab(shell!.ContentGrid, "NavHome").Should().BeTrue(
				"the initial (default-tab) navigation must resume after the content is re-attached");

			// Navigate away — this is expected to work even when the bug is present.
			phase = "select-menu";
			shell.NavView.SelectedItem = shell.MenuItem;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavMenu"), cts.Token);
			}

			// The reported bug: navigating BACK to the default tab does nothing.
			phase = "select-home";
			shell.NavView.SelectedItem = shell.HomeItem;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				try
				{
					await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavHome"), cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
			}

			IsOnlyVisibleTab(shell.ContentGrid, "NavHome").Should().BeTrue(
				"after a startup re-graft, selecting the default tab must still navigate back to it");

			initCompleted.Should().BeTrue(
				"the initial navigation must complete (not hang) when the content detaches mid-cascade");
		}
		catch (ArgumentOutOfRangeException ex)
		{
			// Known Uno.UI defect: NavigationView.GetIndexPathForContainer crashes when a
			// selection change involves an item container realized before the re-graft. The
			// crash fires inside OnSelectedItemPropertyChanged BEFORE SelectionChanged is
			// raised, so the SelectorNavigator is never notified — user-visible as "clicking
			// the default tab does nothing" while the selection indicator still moves.
			Assert.Fail($"Uno.UI NavigationView container crash during phase '{phase}': {ex}");
		}
		finally
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
		}
	}

	/// <summary>
	/// The hang itself: when the hosting tree detaches while the initial navigation is
	/// cascading into the default tab, the navigation pipeline must complete (parking the
	/// undeliverable part) rather than dangle forever. PanelVisiblityNavigator's
	/// CheckLoadedAsync waits unbounded on the detached FrameView (unlike FrameNavigator /
	/// ContentControlNavigator which use the host-aware wait from spec 006), so today this
	/// hangs until the content happens to return to the tree — which a re-hosting preview
	/// host never does in place.
	/// </summary>
	[TestMethod]
	public async Task When_TreeDetachedDuringStartup_Then_InitialNavigationDoesNotHang(CancellationToken ct)
	{
		var window = new Window();

		var initTask = window.InitializeNavigationAsync(
			buildHost: () => Task.FromResult(UnoHost
				.CreateDefaultBuilder(typeof(Given_NavViewShellNavigation).Assembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<NavViewShellPage>(),
							new ViewMap<NavHomePage>(),
							new ViewMap<NavMenuPage>(),
							new ViewMap<NavOrdersPage>());

						routes.Register(
							new RouteMap("NavMain", View: views.FindByView<NavViewShellPage>(), IsDefault: true,
								Nested: new RouteMap[]
								{
									new RouteMap("NavHome", View: views.FindByView<NavHomePage>(), IsDefault: true),
									new RouteMap("NavMenu", View: views.FindByView<NavMenuPage>()),
									new RouteMap("NavOrders", View: views.FindByView<NavOrdersPage>()),
								}));
					})
				.Build()),
			initialRoute: "NavMain");

		var root = window.Content as ContentControl;
		root.Should().NotBeNull("Window.Content should be the navigation root ContentControl");

		try
		{
			NavViewShellPage? shell = null;
			Frame? homeFrame = null;
			var detachedDuringLoad = false;

			void OnHomeFrameLoaded(object s, RoutedEventArgs e)
			{
				homeFrame!.Loaded -= OnHomeFrameLoaded;
				detachedDuringLoad = true;
				window.Content = new Border();
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() =>
				{
					shell ??= ResolveShellPage<NavViewShellPage>(root!);
					if (shell is not null && homeFrame is null &&
						FindTabFrameView(shell.ContentGrid, "NavHome")?.Content is Frame f)
					{
						homeFrame = f;
						if (!f.IsLoaded)
						{
							f.Loaded += OnHomeFrameLoaded;
						}
						else
						{
							detachedDuringLoad = true;
							window.Content = new Border();
						}
					}

					return homeFrame is not null;
				}, cts.Token);
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() => detachedDuringLoad, cts.Token);
			}

			detachedDuringLoad.Should().BeTrue("the detach must observe the load (scenario precondition)");

			// The content is NEVER re-attached (a re-hosting preview host moves it elsewhere).
			// The initial navigation must still complete — a dangling navigation pipeline is
			// the defect.
			var initCompleted = await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(15), ct)) == initTask;

			initCompleted.Should().BeTrue(
				"the initial navigation must complete (not hang) when the hosting tree detaches mid-cascade");
		}
		finally
		{
			if (initTask.IsCompletedSuccessfully)
			{
				await (await initTask).StopAsync();
			}
		}
	}

	/// <summary>
	/// Moved re-graft (the production preview-host topology): the app content is not
	/// re-attached in place — it is re-hosted under a NEW parent element while the initial
	/// navigation is still cascading into the default tab. After the move the app must
	/// recover fully: default tab shows, navigating away works, and navigating BACK to the
	/// default tab works.
	/// </summary>
	[TestMethod]
	public async Task When_ContentMovedToNewHostDuringStartup_Then_DefaultTabStillReachable(CancellationToken ct)
	{
		var window = new Window();

		var initTask = window.InitializeNavigationAsync(
			buildHost: () => Task.FromResult(UnoHost
				.CreateDefaultBuilder(typeof(Given_NavViewShellNavigation).Assembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<NavViewShellPage>(),
							new ViewMap<NavHomePage>(),
							new ViewMap<NavMenuPage>(),
							new ViewMap<NavOrdersPage>());

						routes.Register(
							new RouteMap("NavMain", View: views.FindByView<NavViewShellPage>(), IsDefault: true,
								Nested: new RouteMap[]
								{
									new RouteMap("NavHome", View: views.FindByView<NavHomePage>(), IsDefault: true),
									new RouteMap("NavMenu", View: views.FindByView<NavMenuPage>()),
									new RouteMap("NavOrders", View: views.FindByView<NavOrdersPage>()),
								}));
					})
				.Build()),
			initialRoute: "NavMain");

		var root = window.Content as ContentControl;
		root.Should().NotBeNull("Window.Content should be the navigation root ContentControl");

		IHost? host = null;
		var phase = "setup";
		try
		{
			NavViewShellPage? shell = null;
			Frame? homeFrame = null;
			var movedDuringLoad = false;

			void OnHomeFrameLoaded(object s, RoutedEventArgs e)
			{
				homeFrame!.Loaded -= OnHomeFrameLoaded;

				// Move (not detach/re-attach): re-host the app content under a fresh
				// wrapper element, like a preview host grafting the app into its own
				// chrome. The old attachment point never comes back.
				movedDuringLoad = true;
				window.Content = null;
				window.Content = new Border { Child = root };
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() =>
				{
					shell ??= ResolveShellPage<NavViewShellPage>(root!);
					if (shell is not null && homeFrame is null &&
						FindTabFrameView(shell.ContentGrid, "NavHome")?.Content is Frame f)
					{
						homeFrame = f;
						if (!f.IsLoaded)
						{
							f.Loaded += OnHomeFrameLoaded;
						}
						else
						{
							movedDuringLoad = true;
							window.Content = null;
							window.Content = new Border { Child = root };
						}
					}

					return homeFrame is not null;
				}, cts.Token);
			}

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() => movedDuringLoad, cts.Token);
			}

			movedDuringLoad.Should().BeTrue("the move must observe the load (scenario precondition)");

			phase = "await-init";
			var initCompleted = await Task.WhenAny(initTask, Task.Delay(TimeSpan.FromSeconds(15), ct)) == initTask;
			if (initCompleted)
			{
				host = await initTask;
			}

			// Recovery under the new host: the default tab must (eventually) be shown.
			phase = "wait-home-initial";
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				try
				{
					await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell!.ContentGrid, "NavHome"), cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
			}

			IsOnlyVisibleTab(shell!.ContentGrid, "NavHome").Should().BeTrue(
				"the initial (default-tab) navigation must complete after the content moves to its new host");

			// Navigate away, then back to the default tab — the reported bug.
			phase = "select-menu";
			shell.NavView.SelectedItem = shell.MenuItem;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavMenu"), cts.Token);
			}

			phase = "select-home";
			shell.NavView.SelectedItem = shell.HomeItem;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				try
				{
					await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "NavHome"), cts.Token);
				}
				catch (OperationCanceledException)
				{
				}
			}

			IsOnlyVisibleTab(shell.ContentGrid, "NavHome").Should().BeTrue(
				"after being re-hosted, selecting the default tab must still navigate back to it");

			initCompleted.Should().BeTrue(
				"the initial navigation must complete (not hang) when the content moves to a new host mid-cascade");
		}
		catch (ArgumentOutOfRangeException ex)
		{
			// Known Uno.UI defect: NavigationView.GetIndexPathForContainer crashes when a
			// selection change involves an item container realized before the re-graft. The
			// crash fires inside OnSelectedItemPropertyChanged BEFORE SelectionChanged is
			// raised, so the SelectorNavigator is never notified — user-visible as "clicking
			// the default tab does nothing" while the selection indicator still moves.
			Assert.Fail($"Uno.UI NavigationView container crash during phase '{phase}': {ex}");
		}
		finally
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
		}
	}

	/// <summary>
	/// Sibling layout (TabbedMainPage): same user gesture — select TabB via the NavigationView,
	/// then select TabA (the IsDefault route) again. Differentiates whether the nested layout is
	/// the trigger: if this passes while the nested tests fail, the nesting is the culprit.
	/// </summary>
	[TestMethod]
	public async Task When_SiblingShell_SelectOtherThenDefault_Then_DefaultContentShown(CancellationToken ct)
	{
		var window = new Window();

		var host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_NavViewShellNavigation).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TabbedMainPage>(),
								new ViewMap<TabAPage>(),
								new ViewMap<TabBPage>());

							routes.Register(
								new RouteMap("TabbedMain", View: views.FindByView<TabbedMainPage>(), IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("TabA", View: views.FindByView<TabAPage>(), IsDefault: true),
										new RouteMap("TabB", View: views.FindByView<TabBPage>()),
									}));
						})
					.Build();
				return h;
			},
			initialRoute: "TabbedMain");

		try
		{
			var root = (ContentControl)window.Content!;

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(Timeout);

			TabbedMainPage? shell = null;
			await UIHelper.WaitFor(() =>
			{
				shell = ResolveShellPage<TabbedMainPage>(root);
				return shell is not null;
			}, cts.Token);

			await UIHelper.WaitFor(() => IsTabVisible(shell!.ContentGrid, "TabA"), cts.Token);

			var tabB = shell!.TabSelector.MenuItems.OfType<NavigationViewItem>()
				.First(item => Region.GetName(item) == "TabB");
			shell.TabSelector.SelectedItem = tabB;

			await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "TabB"), cts.Token);

			var tabA = shell.TabSelector.MenuItems.OfType<NavigationViewItem>()
				.First(item => Region.GetName(item) == "TabA");
			shell.TabSelector.SelectedItem = tabA;

			using var backCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			backCts.CancelAfter(Timeout);
			try
			{
				await UIHelper.WaitFor(() => IsOnlyVisibleTab(shell.ContentGrid, "TabA"), backCts.Token);
			}
			catch (OperationCanceledException)
			{
			}

			IsOnlyVisibleTab(shell.ContentGrid, "TabA").Should().BeTrue(
				"selecting the default tab again must switch the content region back to it");
		}
		finally
		{
			await host.StopAsync();
		}
	}
}
