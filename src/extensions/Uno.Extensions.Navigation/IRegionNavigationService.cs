using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationService : INavigationService
{
    void Attach(IRegionNavigationService childRegion, string regionName);

    void Detach(IRegionNavigationService childRegion);
}
