using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public interface INavigationStop
    {
        Task Stop(NavigationContext context, bool cleanup);
    }

    public interface INavigationAware
    {
        INavigationService Navigation { get; set; }
    }
}
