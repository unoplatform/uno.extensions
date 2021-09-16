using Uno.Extensions.Navigation.Controls;
using Uno.Extensions.Navigation.Dialogs;

namespace Uno.Extensions.Navigation.Adapters;

public class SimpleNavigationAdapter<TControl> : BaseNavigationAdapter<TControl>
{
    private NavigationContext currentContext;

    protected override NavigationContext CurrentContext => currentContext;

    public override bool CanGoBack => false;

    public override bool IsCurrentPath(string path)
    {
        return CurrentContext?.Path == path;
    }

    public SimpleNavigationAdapter(IViewManager<TControl> control, IDialogFactory dialogFactory) : base(control, dialogFactory)
    {
    }

    protected override void AdapterNavigation(NavigationContext context, object viewModel)
    {
        currentContext = context;
        ControlWrapper.ChangeView(context, false, viewModel);
    }
}
