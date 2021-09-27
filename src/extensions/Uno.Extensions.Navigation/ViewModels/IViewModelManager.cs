using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels
{
    public interface IViewModelManager
    {
        void CreateViewModel(NavigationContext context);

        Task InitializeViewModel(NavigationContext context);

        Task StartViewModel(NavigationContext context);

        Task StopViewModel(NavigationContext context);

        void DisposeViewModel(NavigationContext context);
    }
}
