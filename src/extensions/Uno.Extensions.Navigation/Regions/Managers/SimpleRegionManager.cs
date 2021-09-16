using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation.Regions.Managers;

public class SimpleRegionManager<TControl> : BaseRegionManager<TControl>
{
    private NavigationContext currentContext;

    protected override NavigationContext CurrentContext => currentContext;

    public override bool CanGoBack => false;

    public override bool IsCurrentPath(string path)
    {
        return CurrentContext?.Path == path;
    }

    public SimpleRegionManager(IViewManager<TControl> control, IDialogFactory dialogFactory) : base(control, dialogFactory)
    {
    }

    protected override void AdapterNavigation(NavigationContext context, object viewModel)
    {
        currentContext = context;
        ControlWrapper.ChangeView(context.Path, context.Mapping?.View, false, context.Data, viewModel, context.Request.Sender is not null);
    }
}
