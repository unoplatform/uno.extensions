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
    public class CompositeRegion : BaseRegion
    {
        internal List<IRegionNavigate> Regions { get; } = new List<IRegionNavigate>();

        public CompositeRegion(
            ILogger<CompositeRegion> logger,
            INavigationService navigation,
            IViewModelManager viewModelManager,
            IDialogFactory dialogFactory) : base(logger, navigation, viewModelManager, dialogFactory)
        {
        }

        private NavigationContext currentContext;

        protected override NavigationContext CurrentContext => currentContext;

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
