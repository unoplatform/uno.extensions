using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation.Regions.Managers;

public class SimpleRegionManager<TControl> : BaseRegionManager
    where TControl : IViewManager
{
    private TControl Control { get; }

    private NavigationContext currentContext;

    protected override NavigationContext CurrentContext => currentContext;

    public SimpleRegionManager(INavigationService navigation, IDialogFactory dialogFactory, TControl control) : base(navigation, dialogFactory)
    {
        Control = control;
    }

    protected override void RegionNavigate(NavigationContext context, object viewModel)
    {
        currentContext = context;
        Control.Show(context.Path, context.Mapping?.View, context.Data, viewModel);
    }
}
