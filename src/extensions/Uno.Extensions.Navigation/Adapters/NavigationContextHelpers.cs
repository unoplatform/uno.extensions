using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Adapters;

public static class NavigationContextHelpers
{
    public static async Task<object> StopVieModel(this NavigationContext contextToStop, NavigationContext navigationContext)
    {
        object oldVm = default;
        if (contextToStop.Mapping?.ViewModel is not null)
        {
            var services = contextToStop.Services;
            oldVm = services.GetService(contextToStop.Mapping.ViewModel);
            await ((oldVm as INavigationStop)?.Stop(navigationContext, navigationContext.IsBackNavigation) ?? Task.CompletedTask);
        }
        return oldVm;
    }

    public static async Task<object> InitializeViewModel(this NavigationContext contextToInitialize)
    {
        var mapping = contextToInitialize.Mapping;
        object vm = default;
        if (mapping?.ViewModel is not null)
        {
            var services = contextToInitialize.Services;
            var dataFactor = services.GetService<ViewModelDataProvider>();
            dataFactor.Parameters = contextToInitialize.Data;

            vm = services.GetService(mapping.ViewModel);
            await ((vm as IInitialise)?.Initialize(contextToInitialize) ?? Task.CompletedTask);
        }
        return vm;
    }

    public static async Task StartViewModel(this NavigationContext contextToStart, object currentVM)
    {
        await ((currentVM as INavigationStart)?.Start(contextToStart, false) ?? Task.CompletedTask);
    }
}
