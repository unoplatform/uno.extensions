using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegion
{
    INavigationService Navigation { get; }

    Task AddRegion(string regionName, IRegion childRegion);

    void RemoveRegion(IRegion childRegion);
}
