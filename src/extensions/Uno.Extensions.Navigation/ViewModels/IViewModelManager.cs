using System.Threading.Tasks;

namespace Uno.Extensions.Navigation.ViewModels
{
    public interface IViewModelManager
    {
        object CreateViewModel(NavigationContext context);

        Task StartViewModel(NavigationContext context, object viewModel);

        Task StopViewModel(NavigationContext context, object viewModel);

        void DisposeViewModel(object viewModel);
    }
}
