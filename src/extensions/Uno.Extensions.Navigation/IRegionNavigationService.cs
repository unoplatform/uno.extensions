using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationService : INavigationService
{
    IRegionNavigationService Parent { get; set; }

    void Attach(string regionName, IRegionNavigationService childRegion);

    void Detach(IRegionNavigationService childRegion);
}
