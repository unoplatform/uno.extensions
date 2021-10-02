using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions
{
    public class DialogRegion : BaseRegion
    {
        protected override bool CanGoBack => true;

        protected Stack<Dialog> OpenDialogs { get; } = new Stack<Dialog>();

        private IDialogFactory DialogFactory { get; }

        public DialogRegion(
            ILogger<DialogRegion> logger,
            IServiceProvider scopedServices,
            INavigationService navigation,
            IViewModelManager viewModelManager,
            IDialogFactory dialogFactory) : base(logger, scopedServices, navigation, viewModelManager)
        {
            DialogFactory = dialogFactory;
        }

        //protected override async Task DoNavigation(NavigationContext context)
        //{
        //    // If this is back navigation, then make sure it's used to close
        //    // any of the open dialogs
        //    if (context.IsBackNavigation && OpenDialogs.Any())
        //    {
        //        await CloseDialog(context);
        //    }

        //    var dialog = DialogFactory.CreateDialog(Navigation, context);
        //    if (dialog is not null)
        //    {
        //        OpenDialogs.Push(dialog);
        //    }
        //}

        public override async Task RegionNavigate(NavigationContext context)
        {
            // If this is back navigation, then make sure it's used to close
            // any of the open dialogs
            if (context.IsBackNavigation && OpenDialogs.Any())
            {
                await CloseDialog(context);
            }
            var vm = ViewModelManager.CreateViewModel(context);
            var dialog = DialogFactory.CreateDialog(context, vm);
            if (dialog is not null)
            {
                OpenDialogs.Push(dialog);
            }
        }

        protected async Task CloseDialog(NavigationContext navigationContext)
        {
            var dialog = OpenDialogs.Pop();

            var responseData = navigationContext.Components.Parameters.TryGetValue(string.Empty, out var response) ? response : default;

            await ViewModelManager.StopViewModel(navigationContext, CurrentViewModel);

            ViewModelManager.DisposeViewModel(navigationContext);

            responseData = dialog.Manager.CloseDialog(dialog, navigationContext, responseData);

            // Handle closing dialog with result
            //var completion = dialog.Context.ResultCompletion;
            //if (completion is not null)
            //{
            //    if (dialog.Context.Request.Result is not null && responseData is not null)
            //    {
            //        completion.SetResult(Options.Option.Some<object>(responseData));
            //    }
            //    else
            //    {
            //        completion.SetResult(Options.Option.None<object>());
            //    }
            //}
        }
    }
}
