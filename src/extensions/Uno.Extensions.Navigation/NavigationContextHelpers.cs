using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation
    ;

public static class NavigationContextHelpers
{
    public static async Task<object> StopVieModel(this NavigationContext contextToStop, NavigationContext navigationContext)
    {
        object oldVm = default;
        if (contextToStop.Mapping?.ViewModel is not null)
        {
            var services = contextToStop.Services;
            oldVm = services.GetService(contextToStop.Mapping.ViewModel);
            await ((oldVm as IViewModelStop)?.Stop(navigationContext, navigationContext.IsBackNavigation) ?? Task.CompletedTask);
        }
        return oldVm;
    }

    public static async Task<object> InitializeViewModel(this NavigationContext contextToInitialize, INavigationService navigation)
    {
        var mapping = contextToInitialize.Mapping;
        object vm = default;
        if (mapping?.ViewModel is not null)
        {
            var services = contextToInitialize.Services;
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = contextToInitialize.Data;

            vm = services.GetService(mapping.ViewModel);
            if (vm is INavigationAware navAware)
            {
                navAware.Navigation = navigation;
            }
            await ((vm as IViewModelInitialize)?.Initialize(contextToInitialize) ?? Task.CompletedTask);
        }
        return vm;
    }

    public static async Task StartViewModel(this NavigationContext contextToStart, object currentVM)
    {
        await ((currentVM as IViewModelStart)?.Start(contextToStart, false) ?? Task.CompletedTask);
    }
}
