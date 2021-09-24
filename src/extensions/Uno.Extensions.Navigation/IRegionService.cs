using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionService
{
    Task NavigateAsync(NavigationContext context);
}
