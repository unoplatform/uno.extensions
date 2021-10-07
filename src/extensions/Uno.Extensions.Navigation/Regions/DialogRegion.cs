using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions
{
    public abstract class DialogRegion : BaseRegion
    {
        protected override bool CanGoBack => true;

        private Dialog OpenDialog { get; set; }

        protected override string CurrentPath => this.GetType().Name ?? string.Empty;

        public DialogRegion(
            ILogger<DialogRegion> logger,
            IServiceProvider scopedServices,
            INavigationService navigation,
            IViewModelManager viewModelManager) : base(logger, scopedServices, navigation, viewModelManager)
        {
        }

        public override async Task RegionNavigate(NavigationContext context)
        {
            // If this is back navigation, then make sure it's used to close
            // any of the open dialogs
            if (context.IsBackNavigation && OpenDialog is not null)
            {
                await CloseDialog(context);
            }
            var vm = ViewModelManager.CreateViewModel(context);
            OpenDialog = DisplayDialog(context, vm);
        }

        protected async Task CloseDialog(NavigationContext navigationContext)
        {
            var dialog = OpenDialog;
            OpenDialog = null;

            var responseData = navigationContext.Request.Segments.Parameters.TryGetValue(string.Empty, out var response) ? response : default;

            await ViewModelManager.StopViewModel(navigationContext, CurrentViewModel);

            ViewModelManager.DisposeViewModel(navigationContext);

            CloseDialog(dialog, navigationContext, responseData);
        }

        protected abstract object CloseDialog(Dialog dialog, NavigationContext context, object responseData);

        protected abstract Dialog DisplayDialog(NavigationContext context, object vm);

    }
}
