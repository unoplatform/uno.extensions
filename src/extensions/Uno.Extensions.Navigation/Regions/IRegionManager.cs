using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.Regions;

public interface IRegionManager
{
    NavigationContext CurrentContext { get; }

    Task NavigateAsync(NavigationContext context);
}
