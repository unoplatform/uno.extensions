using System;
using System.Collections.Generic;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Route(string Scheme, string? Base, string? Path, IDictionary<string, object>? Data)
{
    public static Route Empty => new Route(Schemes.None, null, null, null);

    public static Route PageRoute<TPage>() => PageRoute(typeof(TPage));

    public static Route PageRoute(Type pageType) => PageRoute(pageType.Name);

    public static Route PageRoute(string path) => new Route(Schemes.NavigateForward, path, null, null);

    public static Route NestedRoute(string path) => new Route(Schemes.Nested, path, null, null);

    public override string ToString()
    {
        try
        {
            return $"{Scheme}{Base}{Path}{this.Query()}";
        }
        catch
        {
            return base.ToString() ?? string.Empty;
        }
    }
}
