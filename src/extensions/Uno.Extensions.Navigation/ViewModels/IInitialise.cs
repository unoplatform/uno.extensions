using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels;

public interface IInitialise
{
    Task Initialize(NavigationContext context);
}
