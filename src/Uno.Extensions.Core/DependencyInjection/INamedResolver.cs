namespace Uno.Extensions.DependencyInjection;

internal interface INamedResolver<TService>
{
	public string Name { get; }
	public TService? Resolve(IEnumerable<TService> services);
}
