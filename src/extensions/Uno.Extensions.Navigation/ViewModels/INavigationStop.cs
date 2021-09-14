using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface INavigationStop
{
    Task Stop(NavigationContext context, bool cleanup);
}
