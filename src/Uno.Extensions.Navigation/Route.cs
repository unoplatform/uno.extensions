namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Route(string Qualifier = Qualifiers.None, string? Base = null, string? Path = null, IDictionary<string, object>? Data = null, bool Refresh = false)
{
	internal bool IsInternal { get; set; }

	public static Route Empty => new Route(Qualifiers.None, string.Empty, null, null);

	public static Route PageRoute<TPage>() => PageRoute(typeof(TPage));

	public static Route PageRoute(Type pageType) => PageRoute(pageType.Name);

	public static Route PageRoute(string path) => new Route(Qualifiers.None, path, null, null);

	public static Route NestedRoute<TView>() => NestedRoute(typeof(TView));

	public static Route NestedRoute(Type viewType) => NestedRoute(viewType.Name);

	public static Route NestedRoute(string path) => new Route(Qualifiers.Nested, path, null, null);

	/// <summary>
	/// Returns the current Route as <see langword="string"/> 
	/// </summary>
	/// <value>
	/// The navigated Route with this pattern: <see cref="Qualifier"/> <see cref="Base"/> <see cref="Path"/> Route.Query()<br/>
	/// or <see cref="string.Empty"/> if this would be null</value>
	public override string ToString()
	{
		try
		{
			return $"{Qualifier}{Base}{Path}{this.Query()}";
		}
		catch
		{
			return base.ToString() ?? string.Empty;
		}
	}
}
