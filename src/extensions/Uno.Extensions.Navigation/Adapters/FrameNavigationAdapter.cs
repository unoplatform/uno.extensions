using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation.Adapters
{
    public class FrameNavigationAdapter : INavigationAdapter
    {
        public const string PreviousViewUri = "..";

        private IFrameWrapper NavigationFrame { get; }

        private INavigationMapping Mapping { get; }

        public FrameNavigationAdapter(IFrameWrapper frameWrapper, INavigationMapping navigationMapping)
        {
            NavigationFrame = frameWrapper;
            Mapping = navigationMapping;
        }

        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var isRooted = path.StartsWith("/");

            var segments = path.Split('/');
            var numberOfPagesToRemove = 0;
            var navPath = string.Empty;
            for (int i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(segments[i]))
                {
                    continue;
                }
                if (segments[i] == PreviousViewUri)
                {
                    numberOfPagesToRemove++;
                }
                else
                {
                    navPath = segments[i];
                }
            }

            bool removeCurrentPageFromBackStack = numberOfPagesToRemove > 0;
            // If nav back, we need to remove one less page from stack
            // If nav forward, we need to remove the current page from stack after navigation
            numberOfPagesToRemove--;
            if (navPath == string.Empty)
            {
                navPath = PreviousViewUri;
                removeCurrentPageFromBackStack = false;
            }



            while (numberOfPagesToRemove > 0)
            {
                NavigationFrame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }

            if (navPath == PreviousViewUri)
            {
                NavigationFrame.GoBack();
            }
            else
            {

                var navigationType = Mapping.LookupByPath(navPath);

                var success = NavigationFrame.Navigate(navigationType.View);

                if (isRooted)
                {
                    NavigationFrame.ClearBackStack();
                }

                if (removeCurrentPageFromBackStack)
                {
                    NavigationFrame.RemoveLastFromBackStack();
                }
            }

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
