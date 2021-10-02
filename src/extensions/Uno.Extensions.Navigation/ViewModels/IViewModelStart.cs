using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface IViewModelStart
{
    Task Start(NavigationRequest request);
}
