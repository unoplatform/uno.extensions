namespace Uno.Extensions.Navigation.Controls;

public interface IViewManager<TControl> : IInjectable
{
    void ChangeView(NavigationContext context, bool isBackNavigation, object viewModel);
}
