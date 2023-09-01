namespace Uno.Extensions.Maui.Internals;

internal class LinkedServiceProvider : IServiceProvider, IDisposable
{
	private readonly IServiceProvider _parent;
	public readonly IServiceProvider _child;

	public LinkedServiceProvider(IServiceProvider parent, IServiceProvider child)
	{
		_parent = parent;
		_child = child;
	}

	public object? GetService(Type serviceType)
	{
		if (serviceType == typeof(IServiceScopeFactory))
		{
			return new LinkedServiceScopeFactory(_parent, _child);
		}

		// Note that this will fail if the serviceType exists in either Parent or Child
		// but any of it dependencies exist in the other ServiceProvider. This fails because
		// internally the container has optimizations for invoking constructors that doesn't
		// call GetService for each argument (ie so the fallback to child never happens).
		// TODO: Find a fallback solution that finds each constructor argument and fetches
		// them before invoking the constructor.
		return _parent.GetService(serviceType) ?? _child.GetService(serviceType);
	}

	public void Dispose()
	{
		if (_parent is IDisposable disposableChild)
		{
			disposableChild.Dispose();
		}
	}
}
