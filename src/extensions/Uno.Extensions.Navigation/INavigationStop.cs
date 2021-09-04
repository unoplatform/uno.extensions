using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public interface INavigationStop
    {
        Task Stop(INavigationContext context, bool cleanup);
    }

    public interface INavigationAware
    {
        INavigationService Navigation { get; set; }
    }
}
