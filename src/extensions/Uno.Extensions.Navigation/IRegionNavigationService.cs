using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationService : INavigationService
{
    Task Attach(string regionName, IRegionNavigationService childRegion);

    void Detach(IRegionNavigationService childRegion);
}
