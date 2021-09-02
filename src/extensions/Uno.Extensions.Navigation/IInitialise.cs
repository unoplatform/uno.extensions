using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public interface IInitialise
    {
        Task Initialize(INavigationContext context);
    }
}
