using System.Collections.Generic;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record RouteSegments(string NavigationPath, bool IsRooted, int NumberOfPagesToRemove, IDictionary<string, object> Parameters, NavigationRequest NextRequest)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
}
