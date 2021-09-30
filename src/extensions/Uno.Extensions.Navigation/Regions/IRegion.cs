using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegion : IRegionNavigate
{
    NavigationResponse NavigateAsync(NavigationRequest context);
}

public interface IRegionNavigate
{
    Task RegionNavigate(NavigationContext context);
}
