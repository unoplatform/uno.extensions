namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Route(string Qualifier, string? Base = null, string? Path = null, IDictionary<string, object>? Data = null)
{
	internal  bool IsInternal { get; set; }

	public static Route Empty => new Route(Qualifiers.None, null, null, null);

	public static Route PageRoute<TPage>() => PageRoute(typeof(TPage));

	public static Route PageRoute(Type pageType) => PageRoute(pageType.Name);

	public static Route PageRoute(string path) => new Route(Qualifiers.NavigateForward, path, null, null);

	public static Route NestedRoute<TView>() => NestedRoute(typeof(TView));

	public static Route NestedRoute(Type viewType) => NestedRoute(viewType.Name);

	public static Route NestedRoute(string path) => new Route(Qualifiers.Nested, path, null, null);

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
