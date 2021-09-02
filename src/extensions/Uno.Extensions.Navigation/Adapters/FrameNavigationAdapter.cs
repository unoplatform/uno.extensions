﻿using System.Collections.Generic;
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

        private IList<(string, INavigationContext)> NavigationContexts { get; } = new List<(string, INavigationContext)>();

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

        public bool CanNavigate(NavigationContext context)
        {
            var request = context.Request;
            return true;
        }




        public NavigationResult Navigate(NavigationContext context)
        {
            var request = context.Request;

            if (request.Response != null)
            {
                var tcs = new TaskCompletionSource<object>();
                context = context with { ResponseCompletion = tcs };
            }

            var navTask = InternalNavigate(context);

            return new NavigationResult(request, navTask, context.ResponseCompletion?.Task ?? Task.FromResult<object>(null));
        }

        private async Task InternalNavigate(NavigationContext context)
        {
            var request = context.Request;
            var path = request.Route.Path.OriginalString;
            //Debug.WriteLine("Navigation: " + path);

            //var queryIdx = path.IndexOf('?');
            //var query = string.Empty;
            //if (queryIdx >= 0)
            //{
            //    queryIdx++; // Step over the ?
            //    query = queryIdx < path.Length ? path.Substring(queryIdx) : string.Empty;
            //    path = path.Substring(0, queryIdx - 1);
            //}

            //    var paras = ParseQueryParameters(query);
            //    if (request.Route.Data is not null)
            //    {
            //        paras[string.Empty] = request.Route.Data;
            //    }
            //    request = request with { Route = request.Route with { Data = paras } };

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

            // If there's a current nav context, make sure it's stopped before
            // we proceed - this could cancel the navigation, so need to know
            // before we remove anything from backstack
            if (NavigationContexts.Count > 0)
            {
                if(navPath == PreviousViewUri)
                {
                    var previousContext = NavigationContexts.Peek().Item2;
                    if (previousContext.Request.Response is not null)
                    {
                        var completion = (previousContext as NavigationContext)?.ResponseCompletion;
                        if (completion is not null &&
                            (request.Route.Data is IDictionary<string, object> data) &&
                            data.TryGetValue(string.Empty, out var response))
                        {
                            completion.SetResult(response);
                        }
                    }
                }


                var currentVM = await StopCurrentViewModel(context, navPath == PreviousViewUri);
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            while (numberOfPagesToRemove > 0)
            {
                NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                Frame.RemoveLastFromBackStack();
                numberOfPagesToRemove--;
            }

            if (navPath == PreviousViewUri)
            {
                var vm = await InitializeViewModel();
                Frame.GoBack(request.Route.Data, vm);

               

                await ((vm as INavigationStart)?.Start(NavigationContexts.Peek().Item2, false) ?? Task.CompletedTask);

            }
            else
            {
                var mapping = Mapping.LookupByPath(navPath);
                if (mapping is not null)
                {
                    context = context with { Mapping = mapping };
                }

                if(mapping.View?.IsSubclassOf(typeof(ContentDialog))??false)
                {
                    var dialog = Activator.CreateInstance(mapping.View) as ContentDialog;
                    await dialog.ShowAsync();
                    context.ResponseCompletion?.SetResult(null);
                    return;
                }

                // Push the new navigation context
                NavigationContexts.Push((navPath, context));

                var vm = await InitializeViewModel();

                var success = Frame.Navigate(context.Mapping.View, request.Route.Data, vm);
                await ((vm as INavigationStart)?.Start(context, true) ?? Task.CompletedTask);
                if (isRooted)
                {
                    while (NavigationContexts.Count > 1)
                    {
                        NavigationContexts.RemoveAt(0);
                    }

                    Frame.ClearBackStack();
                }

                if (removeCurrentPageFromBackStack)
                {
                    NavigationContexts.RemoveAt(NavigationContexts.Count - 2);
                    Frame.RemoveLastFromBackStack();
                }
            }

        }

        private async Task<object> StopCurrentViewModel(INavigationContext navigation, bool popContext)
        {
            var ctx = NavigationContexts.Peek();
            var path = ctx.Item1;
            var context = ctx.Item2;

            //var mapping = Mapping.LookupByPath(path);
            object oldVm = default;
            if (context.Mapping.ViewModel is not null)
            {
                var services = context.Services;
                oldVm = services.GetService(context.Mapping.ViewModel);
                await ((oldVm as INavigationStop)?.Stop(navigation, false) ?? Task.CompletedTask);
            }
            if (popContext)
            {
                NavigationContexts.Pop();
            }
            return oldVm;
        }

        private async Task<object> InitializeViewModel()
        {
            var ctx = NavigationContexts.Peek();
            var path = ctx.Item1;
            var context = ctx.Item2;

            var mapping = context.Mapping;// Mapping.LookupByPath(path);
            object vm = default;
            if (mapping.ViewModel is not null)
            {
                var services = context.Services;
                var dataFactor = services.GetService<ViewModelDataProvider>();
                dataFactor.Parameters = context.Request.Route.Data as IDictionary<string, object>;

                vm = services.GetService(mapping.ViewModel);
                await ((vm as IInitialise)?.Initialize(context) ?? Task.CompletedTask);
            }
            return vm;
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
