using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation.Regions.Managers;

public class SimpleRegionManager<TControl> : BaseRegionManager<TControl>
    where TControl : IViewManager
{
    private NavigationContext currentContext;

    protected override NavigationContext CurrentContext => currentContext;

    public SimpleRegionManager(INavigationService navigation, TControl control, IDialogFactory dialogFactory) : base(navigation, control, dialogFactory)
    {
    }

    protected override void AdapterNavigation(NavigationContext context, object viewModel)
    {
        currentContext = context;
        ControlWrapper.Show(context.Path, context.Mapping?.View, context.Data, viewModel, context.Request.Sender is not null);
    }
}
