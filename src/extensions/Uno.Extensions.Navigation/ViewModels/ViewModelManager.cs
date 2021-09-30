using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Navigation.ViewModels
{
    public class ViewModelManager : IViewModelManager
    {
        private ILogger Logger { get; }

        private INavigationService Navigation { get; }

        public ViewModelManager(ILogger<ViewModelManager> logger, INavigationService navigation)
        {
            Logger = logger;
            Navigation = navigation;
        }

        public void CreateViewModel(NavigationContext context)
        {
            var services = context.Services;
            var mapping = context.Mapping;
            if (mapping?.ViewModel is not null)
            {
                var dataFactor = services.GetService<ViewModelDataProvider>();
                dataFactor.Parameters = context.Components.Parameters;

                var vm = services.GetService(mapping.ViewModel);
                if (vm is IInjectable<INavigationService> navAware)
                {
                    navAware.Inject(Navigation);
                }

                if (vm is IInjectable<IServiceProvider> spAware)
                {
                    spAware.Inject(context.Services);
                }

            }
        }

        public Task InitializeViewModel(NavigationContext context)
        {
            var services = context.Services;
            var mapping = context.Mapping;
            if (mapping?.ViewModel is not null)
            {
                var vm = services.GetService(mapping.ViewModel);
                return (vm as IViewModelInitialize)?.Initialize(context) ?? Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public Task StartViewModel(NavigationContext context)
        {
            var services = context.Services;
            var mapping = context.Mapping;
            if (mapping?.ViewModel is not null)
            {
                var vm = services.GetService(mapping.ViewModel);
                return (vm as IViewModelStart)?.Start(context) ?? Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public Task StopViewModel(NavigationContext context)
        {
            var services = context.Services;
            var mapping = context.Mapping;
            if (mapping?.ViewModel is not null)
            {
                var vm = services.GetService(mapping.ViewModel);
                return (vm as IViewModelStop)?.Stop(context) ?? Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        public void DisposeViewModel(NavigationContext context)
        {
            var services = context.Services;
            var mapping = context.Mapping;
            if (mapping?.ViewModel is not null)
            {
                var vm = services.GetService(mapping.ViewModel);
                (vm as IDisposable)?.Dispose();
            }
        }
    }
}
