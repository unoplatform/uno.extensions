using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager : IRegionManagerNavigate
{
    NavigationContext CurrentContext { get; }

    Task NavigateAsync(NavigationContext context);
}

public interface IRegionManagerNavigate
{
    void RegionNavigate(NavigationContext context);
}
