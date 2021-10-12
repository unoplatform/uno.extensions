using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record Route(string Scheme, string Base, string Path, IDictionary<string, object> Data)
{
    public bool IsCurrent => Scheme == Schemes.Current || Scheme.StartsWith(Schemes.NavigateForward) || Scheme.StartsWith(Schemes.NavigateBack);

    public bool IsParent => Scheme.StartsWith(Schemes.Parent);

    public bool IsNested => Scheme.StartsWith(Schemes.Nested);

    public bool IsDialog => Scheme.StartsWith(Schemes.Dialog);

    // eg -/NextPage
    public bool FrameIsRooted => Scheme.EndsWith(Schemes.Root + string.Empty);

    private int NumberOfGoBackInScheme => Scheme.TakeWhile(x => x + string.Empty == Schemes.NavigateBack).Count();

    public int FrameNumberOfPagesToRemove => FrameIsBackNavigation ? NumberOfGoBackInScheme - 1 : NumberOfGoBackInScheme;

    // Only navigate back if there is no base. If a base is specified, we do a forward navigate and remove items from the backstack
    public bool FrameIsBackNavigation => Scheme.StartsWith(Schemes.NavigateBack) && Base.Length == 0;

    public NavigationRequest NextRequest(object sender) =>
        Path.Length > 1 ?
        (Schemes.Nested + Path + Query)
        .AsRequest(sender, Data.ContainsKey(string.Empty) ? Data[string.Empty] : null) :
        null;

    private string UriPath => ((Path is { Length: > 0 }) ? "/" : string.Empty) + Path;

    private string Query => Data.Where(x => x.Key != string.Empty).Any() ?
        "?" + string.Join("&", Data.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
        null;

    public Uri Uri => new Uri($"{Scheme}{Base}{UriPath}{Query}", UriKind.Relative);
}
