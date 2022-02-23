namespace Uno.Extensions.Navigation;

public record Resolver(IRouteResolver Routes, IViewResolver Views) : IResolver
{
}
