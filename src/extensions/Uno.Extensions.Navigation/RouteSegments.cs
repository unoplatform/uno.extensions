using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteSegments(string Scheme, string[] Segments, IDictionary<string, object> Parameters)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public bool IsCurrent => Scheme == RouteConstants.Schemes.Current;

    public bool IsParent => Scheme == RouteConstants.Schemes.Parent;

    public bool IsNested => Scheme == RouteConstants.Schemes.Nested;

    public bool IsDialog => Scheme == RouteConstants.Schemes.Dialog;

    private string TrimmedFrameBase = Segments.FirstOrDefault().TrimStart(
        RouteConstants.RelativePath.GoBack,
        RouteConstants.RelativePath.GoForward);

    public string FrameBase => Segments.FirstOrDefault();

    public string Base => TrimmedFrameBase.TrimStart(RouteConstants.RelativePath.Root);

    public bool IsRooted => TrimmedFrameBase.StartsWith(RouteConstants.RelativePath.Root);

    private int NumberOfGoBackInBase => FrameBase.TakeWhile(x => x == RouteConstants.RelativePath.GoBack).Count();
    public int NumberOfPagesToRemove => IsBackNavigation ? NumberOfGoBackInBase - 1 : NumberOfGoBackInBase;

    public bool IsBackNavigation => FrameBase.TrimStart(RouteConstants.RelativePath.GoBack, RouteConstants.RelativePath.Root).Length == 0;

    private string Query => Parameters.Where(x => x.Key != string.Empty).Any() ?
        "?" + string.Join("&", Parameters.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
        null;

    public NavigationRequest NextRequest(object sender) =>
        Segments.Length > 1 ?
        (RouteConstants.Schemes.Nested + "/" + (string.Join("/", Segments[1..]) + Query))
        .AsRequest(sender, Parameters.ContainsKey(string.Empty) ? Parameters[string.Empty] : null) :
        null;
}
