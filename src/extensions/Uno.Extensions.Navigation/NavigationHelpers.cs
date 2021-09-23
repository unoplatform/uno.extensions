using System;

namespace Uno.Extensions.Navigation;

public static class NavigationHelpers
{
    public static NavigationRequest WithPath(this NavigationRequest request, string path, string queryParameters = "")
    {
        return !string.IsNullOrWhiteSpace(path)?null: request with { Route = request.Route with { Uri = new Uri(path + (!string.IsNullOrWhiteSpace(queryParameters) ? $"?{queryParameters}" : string.Empty), UriKind.Relative) } };
    }
}
