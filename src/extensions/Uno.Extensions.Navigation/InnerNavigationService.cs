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
            request = request.MakeCurrentRequest();

            return Navigation.NavigateAsync(request);
        }
    }
}
