namespace Uno.Extensions.Navigation;

public record NavigationConfig (Type? RouteResolver = null, bool? AddressBarUpdateEnabled=null)
{
	public NavigationConfig(): this(null,null) { }
}
