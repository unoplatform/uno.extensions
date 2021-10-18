using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.ViewModels;
using Windows.Foundation;

namespace Uno.Extensions.Navigation.Services
{
    public abstract class DialogNavigator : RegionNavigator
    {
        protected override bool CanGoBack => true;

        private IAsyncInfo ShowTask { get; set; }

        protected override string CurrentPath => GetType().Name ?? string.Empty;

        protected DialogNavigator(
            ILogger<DialogNavigator> logger,
            IRegion region)
            : base(logger, region)
        {
        }

        protected override async Task NavigateWithContextAsync(NavigationContext context)
        {
            // If this is back navigation, then make sure it's used to close
            // any of the open dialogs
            if (context.Request.Route.FrameIsBackNavigation && ShowTask is not null)
            {
                await CloseDialog(context);
                return;
            }
            var vm = context.CreateViewModel();
            ShowTask = DisplayDialog(context, vm);
        }

        protected async Task CloseDialog(NavigationContext navigationContext)
        {
            var dialog = ShowTask;
            ShowTask = null;

            var responseData = navigationContext.Request.Route.Data.TryGetValue(string.Empty, out var response) ? response : default;

            await CurrentViewModel.Stop(navigationContext.Request);

            dialog.Cancel();
        }

        protected abstract IAsyncInfo DisplayDialog(NavigationContext context, object vm);
    }
}
