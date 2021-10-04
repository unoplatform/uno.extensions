using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteSegments(string[] Segments, IDictionary<string, object> Parameters)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public string NavigationPath => Segments.FirstOrDefault();

    public bool IsRooted => Segments.Any() && Segments[0] is null;

    public int NumberOfPagesToRemove => NavigationPath.TakeWhile(x => x == RouteConstants.RelativePath.GoBack).Count();

    private string Query => Parameters.Where(x => x.Key != string.Empty).Any() ?
        "?" + string.Join("&", Parameters.Where(x => x.Key != string.Empty).Select(kvp => $"{kvp.Key}={kvp.Value}")) :
        null;

    public NavigationRequest NextRequest(object sender) =>
        Segments.Length>1?
        (string.Join("/", Segments[1..]) + Query)
        .AsRequest(sender, Parameters.ContainsKey(string.Empty) ? Parameters[string.Empty] : null):
        null;
}
