using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager : INavigationAware
{
    Task NavigateAsync(NavigationContext context);
}
