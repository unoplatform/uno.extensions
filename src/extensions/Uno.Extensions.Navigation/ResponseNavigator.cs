using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public class ResponseNavigator : INavigator
    {
        private INavigator Navigation { get; }

        public TaskCompletionSource<Options.Option> ResultCompletion { get; }

        public Route CurrentRoute => Navigation?.CurrentRoute;

        public ResponseNavigator(INavigator internalNavigation, TaskCompletionSource<Options.Option> completion)
        {
            Navigation = internalNavigation;
            ResultCompletion = completion;
        }

        public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
        {
            if (request.Route.FrameIsBackNavigation())
            {
                ResultCompletion.TrySetResult(request.Route.ResponseData() is not null ? Options.Option.Some<object>(request.Route.ResponseData) : Options.Option.None<object>());
            }

            return Navigation.NavigateAsync(request);
        }
    }
}
