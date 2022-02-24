namespace Uno.Extensions.Navigation;

public interface IResolver
{
	IRouteResolver Routes { get; }
	IViewResolver Views { get; }
}
