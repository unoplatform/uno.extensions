namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// HR target for the VM constructor body logic test (#3083).
/// The method body is modified at runtime to simulate changing
/// computation logic called from a ViewModel constructor.
/// </summary>
internal static class HotReloadVmCtorTarget
{
	internal static string ComputeValue(string input)
	{
		return $"original-{input}";
	}
}
