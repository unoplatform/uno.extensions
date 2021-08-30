using System;
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

    public class FrameWrapper : IFrameWrapper
    {
        public Frame NavigationFrame { get; set; }

        public void GoBack()
        {
            NavigationFrame.GoBack();
        }

        public bool Navigate(Type sourcePageType, object parameter = null)
        {
            Debug.WriteLine("Backstack (Navigate - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            var nav = NavigationFrame.Navigate(sourcePageType, parameter);
            Debug.WriteLine("Backstack (Navigate - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            return nav;
        }

        public void RemoveLastFromBackStack()
        {
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            NavigationFrame.BackStack.RemoveAt(NavigationFrame.BackStack.Count - 1);
            Debug.WriteLine("Backstack (RemoveLastFromBackStack - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
        }

        public void ClearBackStack()
        {
            Debug.WriteLine("Backstack (ClearBackStack - before): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
            NavigationFrame.BackStack.Clear();
            Debug.WriteLine("Backstack (ClearBackStack - after): " + string.Join(",", NavigationFrame.BackStack.Select(x => x.SourcePageType.Name)));
        }
    }

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
