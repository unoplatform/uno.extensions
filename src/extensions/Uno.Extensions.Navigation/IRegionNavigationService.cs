using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationService : INavigationService
{
    Task AddRegion(string regionName, IRegionNavigationService childRegion);

    void RemoveRegion(IRegionNavigationService childRegion);
}
