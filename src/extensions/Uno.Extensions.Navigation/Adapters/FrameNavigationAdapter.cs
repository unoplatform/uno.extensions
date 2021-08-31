using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.Extensions.Navigation.Adapters
{
    public class FrameNavigationAdapter : INavigationAdapter<Frame>
    {
        public const string PreviousViewUri = "..";

        private IFrameWrapper Frame { get; }

        private INavigationMapping Mapping { get; }

        public void Inject(Frame control)
        {
            Frame.Inject(control);
        }
        public FrameNavigationAdapter(IFrameWrapper frameWrapper, INavigationMapping navigationMapping)
        {
            Frame = frameWrapper;
            Mapping = navigationMapping;
        }

        public bool CanNavigate(NavigationRequest request)
        {
            return true;
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
                Frame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }

            if (navPath == PreviousViewUri)
            {
                Frame.GoBack();
            }
            else
            {

                var navigationType = Mapping.LookupByPath(navPath);

                var success = Frame.Navigate(navigationType.View);

                if (isRooted)
                {
                    Frame.ClearBackStack();
                }

                if (removeCurrentPageFromBackStack)
                {
                    Frame.RemoveLastFromBackStack();
                }
            }

            return new NavigationResult(request, Task.CompletedTask);
        }
    }
}
