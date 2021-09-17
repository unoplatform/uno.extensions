using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager
{
    Task NavigateAsync(NavigationContext context);
}
