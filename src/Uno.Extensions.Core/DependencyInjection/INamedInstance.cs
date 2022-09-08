namespace Uno.Extensions.DependencyInjection;

public interface INamedInstance<TService>
{
	public string Name { get; }
	public TService? Get();
	public TService GetRequired();
}
