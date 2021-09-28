using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionService
{
    Task NavigateAsync(NavigationContext context);

    Task AddRegion(string regionName, IRegionService childRegion);

    void RemoveRegion(IRegionService childRegion);
}
