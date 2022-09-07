namespace Uno.Extensions.DependencyInjection;

public interface INamedResolver<TService>
{
	public string Name { get; }
	public TService? Resolve(IServiceProvider services);
	public TService ResolveRequired(IServiceProvider services);
}
