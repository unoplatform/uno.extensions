using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.ViewModels;
using Windows.Foundation;

namespace Uno.Extensions.Navigation.Regions
{
    public abstract class DialogNavigationService : ControlNavigationService
    {
        protected override bool CanGoBack => true;

        private IAsyncInfo ShowTask { get; set; }

        protected override string CurrentPath => this.GetType().Name ?? string.Empty;

        protected DialogNavigationService(
            ILogger<DialogNavigationService> logger,
            IRegionNavigationService parent,
            IRegionNavigationServiceFactory serviceFactory,
            IScopedServiceProvider scopedServices,
            IViewModelManager viewModelManager)
            : base(logger, parent, serviceFactory, scopedServices, viewModelManager)
        {
        }

        public override async Task RegionNavigate(NavigationContext context)
        {
            // If this is back navigation, then make sure it's used to close
            // any of the open dialogs
            if (context.IsBackNavigation && ShowTask is not null)
            {
                await CloseDialog(context);
                return;
            }
            var vm = ViewModelManager.CreateViewModel(context);
            ShowTask = DisplayDialog(context, vm);
        }

        protected async Task CloseDialog(NavigationContext navigationContext)
        {
            var dialog = ShowTask;
            ShowTask = null;

            var responseData = navigationContext.Request.Route.Data.TryGetValue(string.Empty, out var response) ? response : default;

            await ViewModelManager.StopViewModel(navigationContext, CurrentViewModel);

            ViewModelManager.DisposeViewModel(navigationContext);

            dialog.Cancel();
        }

        protected abstract IAsyncInfo DisplayDialog(NavigationContext context, object vm);
    }
}
