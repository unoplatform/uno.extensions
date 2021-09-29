using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionService
{
    INavigationService Navigation { get; }

    Task AddRegion(string regionName, IRegionService childRegion);

    void RemoveRegion(IRegionService childRegion);
}
