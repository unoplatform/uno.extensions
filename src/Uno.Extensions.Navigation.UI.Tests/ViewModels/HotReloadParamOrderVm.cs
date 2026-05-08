#if DEBUG
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

/// <summary>
/// ViewModel with multiple DI-injected parameters for #3082 test.
/// The constructor body calls <see cref="HotReloadParamOrderTarget.Combine"/>
/// with two service-derived values. After HR reverses the parameter order
/// in the Combine call, the result changes.
/// </summary>
internal sealed class HotReloadParamOrderVm
{
	public static string? LastResult { get; set; }

	public string Result { get; }

	public HotReloadParamOrderVm(
		ILogger<HotReloadParamOrderVm> logger,
		INavigator navigator)
	{
		var loggerInfo = logger is not null ? "log" : "nolog";
		var navInfo = navigator is not null ? "nav" : "nonav";
		Result = HotReloadParamOrderTarget.Combine(loggerInfo, navInfo);
		LastResult = Result;
	}
}
#endif
