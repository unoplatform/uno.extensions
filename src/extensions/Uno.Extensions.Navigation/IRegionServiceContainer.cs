using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionServiceContainer
{
    Task AddRegion(string regionName, IRegionServiceContainer childRegion);

    void RemoveRegion(IRegionServiceContainer childRegion);
}
