using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegion : IRegionNavigate
{
    Task NavigateAsync(NavigationContext context);
}

public interface IRegionNavigate
{
    void RegionNavigate(NavigationContext context);
}
