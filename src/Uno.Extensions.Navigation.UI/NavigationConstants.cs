namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Centralized navigation timing constants. See AGENTS.md §9/§10 — non-trivial timeouts
/// live here with their rationale rather than scattered as magic numbers.
/// </summary>
internal static class NavigationConstants
{
	/// <summary>
	/// Maximum wall-clock time <see cref="Navigator.EnsureChildRegionsAreLoaded"/> will wait
	/// for a genuinely-expected child <c>NavigationRegion</c> to attach to its parent's
	/// <c>Children</c> collection before giving up.
	/// </summary>
	/// <remarks>
	/// Child regions attach during their host control's <c>Loaded</c> event
	/// (<c>NavigationRegion.HandleLoading → AssignParent</c>). When the app is hosted inside a
	/// collectible <c>AssemblyLoadContext</c> on WASM (e.g. a live-preview/IDE host), those
	/// <c>Loaded</c> events are dispatched noticeably later than on a normal cold start. Field
	/// captures of that scenario showed ~900 ms between "app loaded" and the first dropped
	/// navigation, so the budget is set comfortably above that. The wait only runs in
	/// the failure window (view loaded, no children yet, and a child is genuinely expected), so
	/// it never penalises the happy path or leaf-page navigations.
	/// </remarks>
	public static readonly TimeSpan ChildRegionAttachWaitBudget = TimeSpan.FromMilliseconds(1500);

	/// <summary>
	/// Delay between attachment-poll iterations in
	/// <see cref="Navigator.EnsureChildRegionsAreLoaded"/>. Matches the 50 ms cadence already
	/// used by <c>SelectorNavigator.DeferredInitialSelectionCheckAsync</c> for consistency, and
	/// keeps the poll cheap while still draining the dispatcher between checks.
	/// </summary>
	public static readonly TimeSpan ChildRegionAttachPollInterval = TimeSpan.FromMilliseconds(50);
}
