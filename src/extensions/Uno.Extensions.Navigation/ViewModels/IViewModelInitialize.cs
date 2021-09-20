using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface IViewModelInitialize
{
    Task Initialize(NavigationContext context);
}
