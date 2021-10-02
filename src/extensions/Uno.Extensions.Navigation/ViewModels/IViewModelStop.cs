using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface IViewModelStop
{
    Task<bool> Stop(NavigationRequest request);
}
