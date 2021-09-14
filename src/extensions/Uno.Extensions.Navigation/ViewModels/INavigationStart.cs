using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface INavigationStart
{
    Task Start(NavigationContext context, bool create);
}
