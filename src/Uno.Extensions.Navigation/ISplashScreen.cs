namespace Uno.Extensions.Navigation;

public interface ISplashScreen: IDeferrable
{
}

public interface IDeferrable
{
	Deferral GetDeferral();
}


public class Deferral
{
	private Action? _callback;
	internal Deferral(Action callback)
	{
		_callback = callback;
	}

	public void Complete()
	{
		var method = _callback;
		_callback = null;
		method?.Invoke();
	}
}

internal class SplashScreen : ISplashScreen
{
	public IDeferrable? DeferralSource { get; set; }

	public Deferral GetDeferral() => DeferralSource!.GetDeferral();
}
