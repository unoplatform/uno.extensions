namespace Uno.Extensions.Navigation.Controls;

public interface ISimpleNavigation<TControl> : IInjectable
{
    void Navigate(NavigationContext context, bool isBackNavigation, object viewModel);
}
