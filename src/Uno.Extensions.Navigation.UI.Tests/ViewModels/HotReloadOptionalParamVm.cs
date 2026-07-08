#if DEBUG
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// ViewModel with an optional constructor parameter (ILogger) for #3081 test.
/// The constructor calls <see cref="HotReloadOptionalParamTarget.ComputeWithOptional"/>
/// which initially ignores the optional param, then after HR uses it.
/// </summary>
internal sealed class HotReloadOptionalParamVm
{
	public static string? LastComputedValue { get; set; }

	public string ComputedValue { get; }

	public HotReloadOptionalParamVm(ILogger<HotReloadOptionalParamVm>? logger = null)
	{
		var optionalInfo = logger is not null ? "logger-present" : "no-logger";
		ComputedValue = HotReloadOptionalParamTarget.ComputeWithOptional("base", optionalInfo);
		LastComputedValue = ComputedValue;
	}
}
#endif
