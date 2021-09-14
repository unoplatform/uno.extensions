namespace Uno.Extensions.Navigation.Controls;

public interface IControlNavigation<TControl> : IInjectable
{
    void Navigate(NavigationContext context, bool isBackNavigation, object viewModel);
}
