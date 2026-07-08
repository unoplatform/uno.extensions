namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// ViewModel for the VM constructor body logic test (#3083).
/// Calls <see cref="HotReloadVmCtorTarget.ComputeValue"/> in the constructor
/// to capture a computed value. After HR changes the target method,
/// new instances produce the updated value.
/// </summary>
public sealed class HotReloadVmCtorVm
{
	public HotReloadVmCtorVm()
	{
		ComputedValue = HotReloadVmCtorTarget.ComputeValue("test");
	}

	public string ComputedValue { get; }
}
