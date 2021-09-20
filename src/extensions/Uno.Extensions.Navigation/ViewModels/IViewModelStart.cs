using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface IViewModelStart
{
    Task Start(NavigationContext context, bool create);
}
