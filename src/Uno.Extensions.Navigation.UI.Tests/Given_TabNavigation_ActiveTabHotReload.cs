using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Regression for "navigate back to a hot-reloaded ACTIVE tab silently no-ops"
/// (spec 008 / nav-hotreload-active-tab-handoff). Shape: NavigationView selector +
/// sibling <c>Region.Navigator="Visibility"</c> content grid + nested default route —
/// the same topology as the TabBar shell in the field report (NavigationView avoids the
/// Toolkit dependency; both derive from <c>SelectorNavigator</c>).
///
/// Deterministic (no dev-server): the active tab's Hot Reload is simulated by reproducing
/// the exact runtime effect Uno's HR pipeline has on an active tab's inner Frame, verified
/// against Uno's <c>ClientHotReloadProcessor</c>:
///   1. <c>SwapViews</c> sets <c>Frame.Content = newPage</c> directly (bypassing
///      <c>Frame.Navigate</c>), detaching the old page — this is what leaves
///      <c>FrameNavigator._content</c> pointing at the old, now-detached page;
///   2. <c>NavigationVisibilityUpdateHandler.RestoreState</c> flags the surviving inner-Frame
///      <c>NavigationRegion</c> via <c>MarkReplacedByHotReload</c>;
///   3. the route cascade is scheduled.
/// The Frame and its <c>Region.Instance</c> survive HR intact (only <c>Frame.Content</c> is
/// swapped), so no fabricated region state is needed.
///
/// Without the fix, navigating back to the hot-reloaded tab hangs in
/// <c>FrameNavigator.CheckLoadedAsync</c> — it awaits <c>EnsureLoadedWhileHostAttached</c> on
/// the stale, detached <c>_content</c>, which never loads while the Frame host stays attached.
/// The navigation never reaches child forwarding, so the tab's <c>FrameNavigator</c> never
/// runs (matching the field report's missing log line) and the tab appears dead.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_TabNavigation_ActiveTabHotReload
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
	private readonly StringBuilder _steps = new();

	private void Step(string s) => _steps.Append(s).Append(" | ");

	[TestMethod]
	public async Task When_NavigateBackToHotReloadedActiveTab_Then_TabReloads()
	{
		var (host, root) = await SetupAsync();

		try
		{
			var tabbedPage = await WaitForAsync(() => GetTabbedMainPage(root), Timeout);
			tabbedPage.Should().NotBeNull("TabbedMainPage should load");
			Step("page-loaded");

			// TabA is the default/visible tab.
			var tabA = await WaitForAsync(() => FindTabFrameView(tabbedPage!, "TabA"), Timeout);
			tabA.Should().NotBeNull("TabA FrameView (default) should be created");
			await WaitForAsync(() => (tabA!.Content as Frame)?.Content as TabAPage, Timeout);
			Step("tabA-initial");

			// Sanity: TabA -> TabB -> TabA works before any Hot Reload.
			await NavAsync(tabbedPage!, tabA!, "TabB", "sanity:A->B");
			var tabB = await WaitForAsync(() => FindTabFrameView(tabbedPage!, "TabB"), Timeout);
			await NavAsync(tabbedPage!, tabB!, "TabA", "sanity:B->A");
			(await WaitForAsync(() => (tabA!.Content as Frame)?.Content as TabAPage, Timeout))
				.Should().NotBeNull("pre-HR: navigating back to TabA must show TabAPage");
			Step("sanity-ok");

			// Simulate a metadata Hot Reload of the currently-visible tab (TabA).
			SimulateActiveTabHotReload(tabA!);
			Step("hr-simulated");

			// Navigate away and back to the hot-reloaded active tab.
			await NavAsync(tabbedPage!, tabA!, "TabB", "post-hr:A->B");
			await WaitForAsync(() => FindTabFrameView(tabbedPage!, "TabB"), Timeout);
			Step("post-hr:on-B");

			await NavAsync(tabbedPage!, tabA!.Navigator!, "TabA", "post-hr:B->A");
			Step("post-hr:back-returned");

			// The hot-reloaded tab must re-render: its FrameView is visible, TabB is collapsed,
			// and the live page is shown. Before the fix, post-hr:B->A above never returns.
			var reloadedTabA = await WaitForAsync(
				() => FindTabFrameView(tabbedPage!, "TabA") is { Visibility: Visibility.Visible } fv
					&& (fv.Content as Frame)?.Content is TabAPage
					? fv : null,
				Timeout);

			reloadedTabA.Should().NotBeNull(
				"navigating back to the hot-reloaded active tab must re-render its page (visible + TabAPage). " +
				$"[steps: {_steps}] " + Diagnostics(tabbedPage!));

			FindTabFrameView(tabbedPage!, "TabB")!.Visibility.Should().Be(Visibility.Collapsed,
				"the previously-shown tab must be collapsed after switching back to the hot-reloaded tab. " +
				$"[steps: {_steps}] " + Diagnostics(tabbedPage!));
		}
		catch (OperationCanceledException)
		{
			throw new AssertFailedException(
				"navigation hung/cancelled reaching back to the hot-reloaded active tab. " +
				$"[steps: {_steps}] " + Diagnostics(GetTabbedMainPage(root)!));
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Reproduces the runtime effect of an Uno XAML/metadata HR on the currently-visible
	/// tab's hosted page. See class remarks for the three-part effect.
	/// </summary>
	private static void SimulateActiveTabHotReload(FrameView tabFrameView)
	{
		if (tabFrameView.Content is not Frame frame)
		{
			throw new InvalidOperationException("FrameView has no inner Frame");
		}

		// (1) SwapViews: HR sets Frame.Content = newPage directly, detaching the old page.
		frame.Content = new TabAPage();

		// (2) NavigationVisibilityUpdateHandler.RestoreState flags the surviving inner-Frame region.
		if (frame.GetInstance() is NavigationRegion region)
		{
			region.MarkReplacedByHotReload();
		}

		// (3) The route cascade is scheduled after the swap.
		NavigationRouteUpdateHandler.ScheduleCascadeForAllContexts();
	}

	/// <summary>
	/// <c>NavigateRouteAsync</c> with a hard timeout so a deadlock surfaces as a diagnostic
	/// failure (with the step trace) rather than hanging the whole test run.
	/// </summary>
	private async Task NavAsync(TabbedMainPage page, object fromOrNav, string toTab, string label)
	{
		var nav = fromOrNav is FrameView fv ? fv.Navigator : (global::Uno.Extensions.Navigation.INavigator)fromOrNav;
		nav.Should().NotBeNull($"{label}: navigator should exist");

		var navTask = nav!.NavigateRouteAsync(page, toTab);
		var completed = await Task.WhenAny(navTask, Task.Delay(Timeout));
		if (completed != navTask)
		{
			throw new AssertFailedException(
				$"{label}: NavigateRouteAsync('{toTab}') did not return within {Timeout.TotalSeconds}s (hang). " +
				$"[steps: {_steps}] " + Diagnostics(page));
		}

		await navTask;
		Step(label);
	}

	private static string Diagnostics(TabbedMainPage page)
	{
		var sb = new StringBuilder();
		sb.Append("[ContentGrid children: ");
		foreach (var fv in page.ContentGrid.Children.OfType<FrameView>())
		{
			var frame = fv.Content as Frame;
			sb.Append($"{{name='{Region.GetName(fv)}', vis={fv.Visibility}, frameContent={frame?.Content?.GetType().Name ?? "<null>"}}} ");
		}
		sb.Append("] ");

		if (page.ContentGrid.GetInstance() is { } contentRegion)
		{
			sb.Append($"[ContentGrid region children ({contentRegion.Children.Count}): ");
			foreach (var child in contentRegion.Children)
			{
				sb.Append($"{{name='{child.Name}', route='{child.Navigator()?.Route?.Base}'}} ");
			}
			sb.Append("]");
		}

		return sb.ToString();
	}

	// ── Harness (mirrors Given_TabNavigation) ───────────────────────────────

	private async Task<(IHost Host, ContentControl Root)> SetupAsync()
	{
		var window = new Window();
		IHost? host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_TabNavigation_ActiveTabHotReload).Assembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<TabbedMainPage>(),
							new ViewMap<TabAPage>(),
							new ViewMap<TabBPage>(),
							new ViewMap<ForwardNavPage>());

						routes.Register(
							new RouteMap("", Nested: new RouteMap[]
							{
								new RouteMap("TabbedMain", View: views.FindByView<TabbedMainPage>(), IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("TabA", View: views.FindByView<TabAPage>(), IsDefault: true),
										new RouteMap("TabB", View: views.FindByView<TabBPage>()),
										new RouteMap("ForwardNav", View: views.FindByView<ForwardNavPage>()),
									}),
							}));
					})
				.Build(),
			initialRoute: "TabbedMain");

		var root = (ContentControl)window.Content!;
		return (host!, root);
	}

	private static TabbedMainPage? GetTabbedMainPage(ContentControl root)
	{
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is TabbedMainPage page)
		{
			return page;
		}
		return root.Content as TabbedMainPage;
	}

	private static FrameView? FindTabFrameView(TabbedMainPage tabbedPage, string tabName)
		=> tabbedPage.ContentGrid.Children
			.OfType<FrameView>()
			.FirstOrDefault(fv => Region.GetName(fv) == tabName || fv.Name == tabName);

	private static async Task<T?> WaitForAsync<T>(Func<T?> probe, TimeSpan timeout) where T : class
	{
		using var cts = new CancellationTokenSource(timeout);
		T? result = null;
		await UIHelper.WaitFor(() => (result = probe()) is not null, cts.Token);
		return result;
	}
}
