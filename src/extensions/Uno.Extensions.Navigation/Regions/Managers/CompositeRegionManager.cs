using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Dialogs;
using Uno.Extensions.Navigation.ViewModels;

namespace Uno.Extensions.Navigation.Regions.Managers
{
    public class CompositeRegionManager : BaseRegionManager
    {
        internal List<IRegionManagerNavigate> Regions { get; } = new List<IRegionManagerNavigate>();

        public CompositeRegionManager(
            ILogger<CompositeRegionManager> logger,
            INavigationService navigation,
            IViewModelManager viewModelManager,
            IDialogFactory dialogFactory) : base(logger, navigation, viewModelManager, dialogFactory)
        {
        }
        private NavigationContext currentContext;

        public override NavigationContext CurrentContext => currentContext;

        public override void RegionNavigate(NavigationContext context)
        {
            currentContext = context;
            foreach (var region in Regions)
            {
                region.RegionNavigate(context);
            }
        }
    }
}
