namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

public sealed class HotReloadRegionVm
{
	public string DisplayedValue => HotReloadRegionTarget.GetValue();

	/// <summary>
	/// Captured once at construction. The #3142 HR test edits <see cref="ComputeCtorSeed"/> and
	/// asserts the active route's view model was RE-INSTANTIATED — a metadata update never
	/// re-runs property initializers on live instances, so only a fresh instance can observe
	/// the post-HR value.
	/// </summary>
	public string CtorSeededValue { get; } = ComputeCtorSeed();

	private static string ComputeCtorSeed()
	{
		return "ctor-original";
	}
}
