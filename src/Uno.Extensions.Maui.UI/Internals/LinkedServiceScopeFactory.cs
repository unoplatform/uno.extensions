namespace Uno.Extensions.Maui.Internals;

internal class LinkedServiceScopeFactory : IServiceScopeFactory
{
	private readonly IServiceProvider _childServiceProvider;
	private readonly IServiceProvider _serviceProvider;

	public LinkedServiceScopeFactory(IServiceProvider serviceProvider, IServiceProvider childServiceProvider)
	{
		_serviceProvider = serviceProvider;
		_childServiceProvider = childServiceProvider;
	}

	public IServiceScope CreateScope()
	{
		var scope = _serviceProvider.CreateScope();
		return new LinkedServiceScope(scope.ServiceProvider, _childServiceProvider);
	}
}
