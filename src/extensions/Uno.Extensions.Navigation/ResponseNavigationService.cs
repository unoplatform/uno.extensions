using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public class ResponseNavigationService : INavigationService
    {
        private INavigationService Navigation { get; }

        public TaskCompletionSource<Options.Option> ResultCompletion { get; set; } = new TaskCompletionSource<Options.Option>();

        public ResponseNavigationService(INavigationService internalNavigation)
        {
            Navigation = internalNavigation;
        }

        public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
        {
            if (request.IsBackRequest())
            {
                ResultCompletion.TrySetResult(request.Route.Data is not null ? Options.Option.Some<object>(request.Route.Data) : Options.Option.None<object>());
            }

            return Navigation.NavigateAsync(request);
        }
    }
}
