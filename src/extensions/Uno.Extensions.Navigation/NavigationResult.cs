using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public record NavigationResult(NavigationRequest Request, Task Response)
    {
    }
}
