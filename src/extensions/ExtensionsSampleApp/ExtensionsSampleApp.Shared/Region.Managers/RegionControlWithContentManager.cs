using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.ViewModels;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.Regions.Managers;
using Uno.Extensions.Navigation.Regions;

namespace ExtensionsSampleApp.Region.Managers
{
    public class RegionControlWithContentRegionManager<TControl, TControlManager, TContent, TContentManager> : SimpleRegionManager<TControl>
           where TControl : class
           where TControlManager : BaseRegionManager<TControl>
           where TContent : class
           where TContentManager : BaseRegionManager<TContent>
    {
        private TControlManager ControlManager { get; }
        private TContentManager ContentManager { get; }
        public RegionControlWithContentRegionManager(
            ILogger<RegionControlWithContentRegionManager<TControl, TControlManager, TContent, TContentManager>> logger,
            TControlManager controlManager,
            TContentManager contentManager,
            INavigationService navigation,
        IViewModelManager viewModelManager,
        IDialogFactory dialogFactory,
        RegionControlProvider controlProvider) : base(logger, navigation, viewModelManager, dialogFactory, controlProvider.RegionControl as TControl)
        {
            ControlManager = controlManager;
            ContentManager = contentManager;
            var controls = (ValueTuple<object, object>)controlProvider.RegionControl;
            this.Control = controls.Item1 as TControl;
            ControlManager.Control = controls.Item1 as TControl;
            ContentManager.Control = controls.Item2 as TContent;
        }

        protected override object InternalShow(string path, Type view, object data, object viewModel)
        {
            //var control = base.InternalShow(path, view, data, viewModel);
            ControlManager.Show(path, view, data, viewModel);
            ContentManager.Show(path, view, data, viewModel);
            return null; // control;
        }
    }
}
