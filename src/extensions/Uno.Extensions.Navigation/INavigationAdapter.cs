using Uno.Extensions.Navigation.Controls;

namespace Uno.Extensions.Navigation
{
    public interface INavigationAdapter<TControl> :INavigationService, IInjectable<TControl>
    {
    }
}
