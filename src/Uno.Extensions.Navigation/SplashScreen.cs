namespace Uno.Extensions.Navigation;

internal class SplashScreen : ISplashScreen
{
	public IDeferrable? DeferralSource { get; set; }

	public IDeferral GetDeferral() => DeferralSource?.GetDeferral() ?? new Deferral(() => { });
}
