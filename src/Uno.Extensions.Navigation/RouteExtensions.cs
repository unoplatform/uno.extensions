namespace Uno.Extensions.Navigation;

public static class RouteExtensions
{
	public static string? Query(this Route route) =>
	((route?.Data?.Where(x => x.Key != string.Empty)?.Any()) ?? false) ?
	"?" + string.Join("&", route.Data.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
	null;
}
