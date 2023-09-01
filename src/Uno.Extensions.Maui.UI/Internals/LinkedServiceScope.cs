namespace Uno.Extensions.Maui.Internals;

public record LinkedServiceScope(IServiceProvider serviceProvider, IServiceProvider childServiceProvider) : IServiceScope
{
	public IServiceProvider ServiceProvider => new LinkedServiceProvider(serviceProvider, childServiceProvider);

	public void Dispose() { }
}
