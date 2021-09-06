using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public interface INavigationStart
    {
        Task Start(NavigationContext context, bool create);
    }
}
