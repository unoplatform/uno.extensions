using System.Threading.Tasks;
#if WINDOWS_UWP || UNO_UWP_COMPATIBILITY
using Windows.UI.Xaml.Controls;
#else
#endif

namespace Uno.Extensions.Navigation
{
    public record NavigationResult(NavigationRequest Request, Task Response)
    {

    }

}
