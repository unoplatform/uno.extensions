using System;

namespace Uno.Extensions.Navigation;

public static class NavigationHelpers
{
    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters)
    {
        return request with { Route = request.Route with { Path = new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative) } };
    }
}
