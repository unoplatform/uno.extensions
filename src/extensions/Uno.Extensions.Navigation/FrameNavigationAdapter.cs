using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation
{
    public class FrameNavigationAdapter : INavigationAdapter
    {
        public const string PreviousViewUri = "..";

        public Frame NavigationFrame { get; set; }

        private INavigationMapping Mapping { get; }

        public FrameNavigationAdapter(INavigationMapping navigationMapping)
        {
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

            Debug.WriteLine("Backstack (before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));

            
            while (numberOfPagesToRemove > 0)
            {
                NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStack.Count - 1);
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
                    NavigationFrame.BackStack.Clear();
                }
                if (removeCurrentPageFromBackStack)
                {
                    NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStack.Count - 1);
                }
            }

            Debug.WriteLine("Backstack (after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
