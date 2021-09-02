using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public interface INavigationStop
    {
        Task Stop(INavigationContext context, bool cleanup);
    }
}
