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
            NavigateDoc1Command = new RelayCommand(NavigateDoc1);
            NavigateThirdPageCommand = new RelayCommand(NavigateThirdPage);
        }



        public ICommand NavigateDoc1Command { get; }
        public ICommand NavigateThirdPageCommand { get; }

        private void NavigateDoc1() => Navigation.NavigateViewModelAsync<TabDoc1ViewModel>(this);

        private void NavigateThirdPage() => Navigation.NavigateViewModelAsync<ThirdViewModel>(this, RouteConstants.RelativePath.Parent(1));

    }

}
