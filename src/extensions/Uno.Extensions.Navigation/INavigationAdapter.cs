using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation
{
    public interface INavigationAdapter<TControl> : INavigationAdapter, IInjectable<TControl>
    {
    }

    public interface INavigationAdapter : INavigationAware
    {
        public string Name { get; set; }

        //bool CanNavigate(NavigationContext context);

        NavigationResult Navigate(NavigationContext context);
    }
}
