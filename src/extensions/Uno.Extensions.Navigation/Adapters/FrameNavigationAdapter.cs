using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Controls;
using System;
using Microsoft.Extensions.DependencyInjection;
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

        private IServiceProvider Services { get; }

        private IList<(string,IServiceProvider)> NavigationViewModelInstances { get; } = new List<(string, IServiceProvider)>();

        public void Inject(Frame control)
        {
            Frame.Inject(control);
        }

        public FrameNavigationAdapter(
            IServiceProvider services,
            INavigationMapping navigationMapping,
            IFrameWrapper frameWrapper)
        {
            Services = services;
            Frame = frameWrapper;
            Mapping = navigationMapping;
        }

        public bool CanNavigate(NavigationRequest request)
        {
            return true;
        }

        private IDictionary<string, object> ParseQueryParameters(string queryString)
        {
            return (from pair in (queryString+string.Empty).Split('&')
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

                var paras = ParseQueryParameters(query);
                if (request.Route.Data is not null)
                {
                    paras[string.Empty] = request.Route.Data;
                }
                request = request with { Route = request.Route with { Data = paras } };

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
                NavigationViewModelInstances.RemoveAt(NavigationViewModelInstances.Count - 2);
                Frame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }

            if (navPath == PreviousViewUri)
            {
                var oldVM = NavigationViewModelInstances.Pop();
                //await((oldVM as ILifecycleStop)?.Stop(true) ?? Task.CompletedTask);
                var sp = NavigationViewModelInstances.Peek();
                var navigationType = Mapping.LookupByPath(sp.Item1);
                object vm = default;
                if (navigationType.ViewModel is not null)
                {
                    var services = sp.Item2;
                    var dataFactor = services.GetService<ViewModelDataProvider>();
                    dataFactor.Parameters = request.Route.Data as IDictionary<string, object>;

                    vm = services.GetService(navigationType.ViewModel);
                    //await((vm as IInitialise)?.Initialize(message.Args) ?? Task.CompletedTask);
                }
                Frame.GoBack(request.Route.Data, vm);
                //await((vm as ILifecycleStart)?.Start(false) ?? Task.CompletedTask);
            }
            else
            {
                if (NavigationViewModelInstances.Count > 0)
                {
                    var oldVM = NavigationViewModelInstances.Peek();
                    //await((oldVM as ILifecycleStop)?.Stop(false) ?? Task.CompletedTask);
                }


                var scope = Services.CreateScope();
                var dataFactor = scope.ServiceProvider.GetService<ViewModelDataProvider>();
                dataFactor.Parameters = request.Route.Data as IDictionary<string, object>;

                var navigationType = Mapping.LookupByPath(navPath);

                object vm = default;
                if (navigationType.ViewModel is not null)
                {
                    vm = scope.ServiceProvider.GetService(navigationType.ViewModel);
                    //await((vm as IInitialise)?.Initialize(message.Args) ?? Task.CompletedTask);
                }

                NavigationViewModelInstances.Push((navPath, scope.ServiceProvider));
                var success = Frame.Navigate(navigationType.View, request.Route.Data, vm);

                if (isRooted)
                {
                    while (NavigationViewModelInstances.Count > 1)
                    {
                        NavigationViewModelInstances.RemoveAt(0);
                    }

                    Frame.ClearBackStack();
                }

                if (removeCurrentPageFromBackStack)
                {
                    NavigationViewModelInstances.RemoveAt(NavigationViewModelInstances.Count - 2);
                    Frame.RemoveLastFromBackStack();
                }
            }

            return new NavigationResult(request, Task.CompletedTask);
        }
    }

    public static class ListHelpers
    {
        public static T Peek<T>(this IList<T> list)
        {
            var t = list.Last();
            return t;
        }

        public static T Pop<T>(this IList<T> list)
        {
            var t = list.Last();
            list.RemoveAt(list.Count - 1);
            return t;
        }

        public static void Push<T>(this IList<T> list, T item)
        {
            list.Add(item);
        }
    }
}
