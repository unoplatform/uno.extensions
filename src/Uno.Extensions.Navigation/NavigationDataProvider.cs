namespace Uno.Extensions.Navigation;

internal class NavigationDataProvider
{
	public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

	public TData? GetData<TData>()
		where TData : class
	{
		return (Parameters?.TryGetValue(string.Empty, out var data) ?? false) ? data as TData : default;
	}
}
