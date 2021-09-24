using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Controls;

namespace ExtensionsSampleApp.Control.Managers
{
    public class RegionControlWithContentManager<TControl, TControlManager, TContent, TContentManager> : BaseControlManager<TControl>
           where TControl : class
           where TControlManager : BaseControlManager<TControl>
           where TContent : class
           where TContentManager : BaseControlManager<TContent>
    {
        private TControlManager ControlManager { get; }
        private TContentManager ContentManager { get; }
        public RegionControlWithContentManager(
            ILogger<PageVisualStateManager> logger,
            INavigationService navigation,
            TControlManager controlManager,
            TContentManager contentManager,
            RegionControlProvider controlProvider) : base(logger, navigation, controlProvider.RegionControl as TControl)
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
