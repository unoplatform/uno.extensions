using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.ViewModels;

public static class ViewModelExtensions
{
    public static Task<bool> Stop(this object viewModel, NavigationRequest request)
    {
        return (viewModel as IViewModelStop)?.Stop(request) ?? Task.FromResult(true);
    }

    public static Task Start(this object viewModel, NavigationRequest request)
    {
        return (viewModel as IViewModelStart)?.Start(request) ?? Task.CompletedTask;
    }

    public static object CreateViewModel(this NavigationContext context)
    {
        var services = context.Services;
        var mapping = context.Mapping;
        if (mapping?.ViewModel is not null)
        {
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = context.Request.Route.Data;

            var vm = services.GetService(mapping.ViewModel);
            if (vm is IInjectable<INavigator> navAware)
            {
                navAware.Inject(context.Navigation);
            }

            if (vm is IInjectable<IServiceProvider> spAware)
            {
                spAware.Inject(context.Services);
            }
            return vm;
        }

        return null;
    }
}
