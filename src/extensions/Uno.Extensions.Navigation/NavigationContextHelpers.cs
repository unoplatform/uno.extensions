using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation
    ;

public static class NavigationContextHelpers
{
    public static NavigationContext BuildNavigationContext(this NavigationRequest request, IServiceProvider services, TaskCompletionSource<Options.Option> completion)
    {
        var path = request.Route.Uri.OriginalString;
        //Logger.LazyLogDebug(() => $"Parsing route '{path}'");

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
            if (request.Route.Data is IDictionary<string, object> paraDict)
            {
                paras.AddRange(paraDict);
            }
            else
            {
                paras[string.Empty] = request.Route.Data;
            }
        }

        var isRooted = path.StartsWith("/");

        var segments = path.Split('/');
        var numberOfPagesToRemove = 0;
        var navPath = string.Empty;
        var residualPath = path;
        var nextPath = string.Empty;
        for (int i = 0; i < segments.Length; i++)
        {
            var navSegment = segments[i];
            residualPath = residualPath.TrimStart(navSegment);
            if (residualPath.StartsWith("/"))
            {
                residualPath = residualPath.Substring(1);
            }
            nextPath = i < segments.Length - 1 ? segments[i + 1] : string.Empty;

            if (string.IsNullOrWhiteSpace(navSegment))
            {
                continue;
            }
            if (segments[i] == NavigationConstants.PreviousViewUri)
            {
                numberOfPagesToRemove++;
            }
            else
            {
                navPath = segments[i];
                break;
            }
        }

        if (navPath == string.Empty)
        {
            navPath = NavigationConstants.PreviousViewUri;
            numberOfPagesToRemove--;
        }

        var residualRequest = request.WithPath(residualPath, query); // with { Route = request.Route with { Path = new Uri(residualPath, UriKind.Relative) } };

        var scopedServices = services.CloneNavigationScopedServices();
        var dataFactor = scopedServices.GetService<ViewModelDataProvider>();
        dataFactor.Parameters = paras;

        var mapping = scopedServices.GetService<INavigationMappings>().LookupByPath(navPath);

        var context = new NavigationContext(
                            scopedServices,
                            request,
                            navPath,
                            isRooted,
                            numberOfPagesToRemove,
                            paras,
                            residualRequest,
                            (request.Cancellation is not null) ?
                                CancellationTokenSource.CreateLinkedTokenSource(request.Cancellation.Value) :
                                new CancellationTokenSource(),
                            completion,
                            mapping);
        return context;
    }

    private static IServiceProvider CloneNavigationScopedServices(this IServiceProvider services)
    {
        var scope = services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        scopedServices.GetService<RegionControlProvider>().RegionControl = services.GetService<RegionControlProvider>().RegionControl;
        scopedServices.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service = services.GetService<ScopedServiceHost<IRegionServiceContainer>>().Service;
        scopedServices.GetService<ScopedServiceHost<INavigationRegionService>>().Service = services.GetService<ScopedServiceHost<INavigationRegionService>>().Service;

        return scopedServices;
    }

    //public static async Task<object> StopVieModel(this NavigationContext contextToStop, NavigationContext navigationContext)
    //{
    //    object oldVm = default;
    //    if (contextToStop.Mapping?.ViewModel is not null)
    //    {
    //        var services = contextToStop.Services;
    //        oldVm = services.GetService(contextToStop.Mapping.ViewModel);
    //        await ((oldVm as IViewModelStop)?.Stop(navigationContext, navigationContext.IsBackNavigation) ?? Task.CompletedTask);
    //    }
    //    return oldVm;
    //}

    //public static async Task<object> InitializeViewModel(this NavigationContext contextToInitialize, INavigationService navigation)
    //{
    //    var mapping = contextToInitialize.Mapping;
    //    object vm = default;
    //    if (mapping?.ViewModel is not null)
    //    {
    //        var services = contextToInitialize.Services;
    //        var dataFactor = services.GetService<ViewModelDataProvider>();
    //        dataFactor.Parameters = contextToInitialize.Data;

    //        vm = services.GetService(mapping.ViewModel);
    //        if (vm is INavigationAware navAware)
    //        {
    //            navAware.Navigation = navigation;
    //        }
    //        await ((vm as IViewModelInitialize)?.Initialize(contextToInitialize) ?? Task.CompletedTask);
    //    }
    //    return vm;
    //}

    //public static async Task StartViewModel(this NavigationContext contextToStart, object currentVM)
    //{
    //    await ((currentVM as IViewModelStart)?.Start(contextToStart, false) ?? Task.CompletedTask);
    //}

    public static object ViewModel(this NavigationContext context)
    {
        var mapping = context.Mapping;
        if (mapping?.ViewModel is not null)
        {
            var services = context.Services;
            return services.GetService(mapping.ViewModel);
        }

        return null;
    }
    private static IDictionary<string, object> ParseQueryParameters(this string queryString)
    {
        return (from pair in (queryString + string.Empty).Split('&')
                where pair is not null
                let bits = pair.Split('=')
                where bits.Length == 2
                let key = bits[0]
                let val = bits[1]
                where key is not null && val is not null
                select new { key, val })
                .ToDictionary(x => x.key, x => (object)x.val);
    }
}
