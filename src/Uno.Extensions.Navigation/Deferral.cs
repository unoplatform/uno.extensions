namespace Uno.Extensions.Navigation;

internal class Deferral : IDeferral
{
	private Action? _callback;
	internal Deferral(Action callback)
	{
		_callback = callback;
	}

	public void Close()
	{
		var method = _callback;
		_callback = null;
		method?.Invoke();
	}
}
