using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;

namespace ExtensionsSampleApp.ViewModels
{
    public class TabDoc0ViewModel
    {
        public string Title => "Doc0";


        private INavigator Navigation { get; }

        private IRouteMappings Mappings { get; }
        public TabDoc0ViewModel(INavigator navigation, IRouteMappings mappings)
        {
            Mappings = mappings;
            Navigation = navigation;
            NavigateToDoc1Command = new RelayCommand(NavigateToDoc1);
            NavigateToThirdPageCommand = new RelayCommand(NavigateToThirdPage);
        }



        public ICommand NavigateToDoc1Command { get; }
        public ICommand NavigateToThirdPageCommand { get; }

        private void NavigateToDoc1() => Navigation.NavigateToViewModelAsync<TabDoc1ViewModel>(this);

        private void NavigateToThirdPage() => Navigation.NavigateToViewModelAsync<ThirdViewModel>(this, RouteConstants.RelativePath.Parent(1));

    }

}
