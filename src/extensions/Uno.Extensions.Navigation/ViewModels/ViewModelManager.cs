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

        public object CreateViewModel(NavigationContext context)
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
                return vm;
            }

            return null;
        }

        public Task StartViewModel(NavigationContext context, object viewModel)
        {
            return (viewModel as IViewModelStart)?.Start(context.Request) ?? Task.CompletedTask;
        }

        public async Task StopViewModel(NavigationContext context, object viewModel)
        {
            var proceed= await ((viewModel as IViewModelStop)?.Stop(context.Request) ?? Task.FromResult(true));
            if (!proceed)
            {
                context.Cancel();
            }
        }

        public void DisposeViewModel(object viewModel)
        {
            (viewModel as IDisposable)?.Dispose();
        }
    }
}
