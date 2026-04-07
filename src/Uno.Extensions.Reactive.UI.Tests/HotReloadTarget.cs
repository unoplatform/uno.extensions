namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// Target class for hot-reload tests. The method body is modified at runtime
/// by <see cref="Given_HotReload"/> via HotReloadHelper.
/// </summary>
internal static class HotReloadTarget
{
	internal static string GetValue() => "original";
}
