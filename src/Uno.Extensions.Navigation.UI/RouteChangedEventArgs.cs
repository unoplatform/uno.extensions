namespace Uno.Extensions.Navigation;

public class RouteChangedEventArgs : EventArgs
{
	public IRegion Region { get; }

	public RouteChangedEventArgs(IRegion region)
	{
		Region = region;
	}
}

