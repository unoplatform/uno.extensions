using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public class InnerNavigationService : INavigationService
    {
        private INavigationService Navigation { get; }

        public InnerNavigationService(INavigationService internalNavigation)
        {
            Navigation = internalNavigation;
        }

        public virtual Task<NavigationResponse> NavigateAsync(NavigationRequest request)
        {
            // a request with no scheme should be treated as a nested request
            // eg "./Tweets" and "Tweets" should be handled the same
            if (request.Route.EmptyScheme)
            {
                request = request with { Route = request.Route.AppendScheme(Schemes.Nested) };
            }

            // ../ should be trimmed off requests so they're processed by the wrapped navigation service
            if (request.Route.IsParent)
            {
                request = request with { Route = request.Route.TrimScheme(Schemes.Parent) };
            }

            return Navigation.NavigateAsync(request);
        }
    }
}
