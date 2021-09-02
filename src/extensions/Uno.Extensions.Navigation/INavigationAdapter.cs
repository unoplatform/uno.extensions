using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation
{
    public interface INavigationAdapter<TControl> : INavigationAdapter, IInjectable<TControl>
    {
    }

    public interface INavigationAdapter
    {
        bool CanNavigate(NavigationContext context);

        NavigationResult Navigate(NavigationContext context);
    }
}
