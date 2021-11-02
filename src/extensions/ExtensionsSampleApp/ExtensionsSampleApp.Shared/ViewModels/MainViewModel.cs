using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtensionsSampleApp.Views;
using Uno.Extensions.Navigation;


namespace ExtensionsSampleApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public string Title => "Main";

        private INavigator Navigation { get; }

        private IRouteMappings Mappings { get; }
        public MainViewModel(INavigator navigation, IRouteMappings mappings)
        {
            Navigation = navigation;
            Mappings = mappings;
            NavigateToSecondPageCommand = new RelayCommand(NavigateToSecondPage);
        }
        public async Task Start(NavigationRequest request)
        {
        }

        public ICommand NavigateToSecondPageCommand { get; }

        private void NavigateToSecondPage() => Navigation.NavigateToViewModelAsync<SecondViewModel>(this, data: new Widget());

        public async Task<bool> Stop(NavigationRequest request)
        {
            if (request.Route.Data.TryGetValue("delay", out var delayAsString))
            {
                await Task.Delay(int.Parse(delayAsString as string));
            }

            return true;
        }

        public List<string> PickerItems { get; } = new List<string>() { "One", "Two", "Three" };

    }

}
