using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using System.Collections.Generic;
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

        private IDictionary<string, object> ParseQueryParameters(string queryString)
        {
            return (from pair in queryString.Split('&')
                    where pair is not null
                    let bits = pair.Split('=')
                    where bits.Length == 2
                    let key = bits[0]
                    let val = bits[1]
                    where key is not null && val is not null
                    select new { key, val })
                    .ToDictionary(x => x.key, x => (object)x.val);
        }


        public NavigationResult Navigate(NavigationRequest request)
        {
            var path = request.Route.Path.OriginalString;
            Debug.WriteLine("Navigation: " + path);

            var queryIdx = path.IndexOf('?');
            var query = string.Empty;
            if (queryIdx >= 0)
            {
                queryIdx++; // Step over the ?
                query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
                path = path.Substring(0, queryIdx - 1);
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                var paras = ParseQueryParameters(query);
                if (paras?.Any() ?? false)
                {
                    if (request.Route.Data is not null)
                    {
                        paras[string.Empty] = request.Route.Data;
                    }
                    request = request with { Route = request.Route with { Data = paras } };
                }
            }


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
                Frame.GoBack(request.Route.Data);
            }
            else
            {

                var navigationType = Mapping.LookupByPath(navPath);

                var success = Frame.Navigate(navigationType.View, request.Route.Data);

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
