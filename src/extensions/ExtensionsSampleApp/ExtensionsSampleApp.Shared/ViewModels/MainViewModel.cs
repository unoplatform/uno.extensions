using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;

namespace ExtensionsSampleApp.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public string Title => "Main";

        private INavigationService Navigation { get; }
        public MainViewModel(INavigationService navigation)
        {
            Navigation = navigation;
            NavigateToSecondPageCommand = new RelayCommand(NavigateToSecondPage);
        }


        public ICommand NavigateToSecondPageCommand { get; }

        private void NavigateToSecondPage() => Navigation.NavigateToViewModel<SecondViewModel>(this);

    }

    public class SecondViewModel
    {
        public string Title => "Second";
    }

    public class ThirdViewModel
    {
        public string Title => "Third";
    }

    public class FourthViewModel
    {
        public string Title => "Fourth";

    }

    public class TabbedViewModel
    {
        public string Title => "Tabbed";
    }

    public class TabDoc0ViewModel
    {
        public string Title => "Doc0";


        private INavigationService Navigation { get; }
        public TabDoc0ViewModel(INavigationService navigation)
        {
            Navigation = navigation;
            NavigateToDoc1Command = new RelayCommand(NavigateToDoc1);
        }


        public ICommand NavigateToDoc1Command { get; }

        private void NavigateToDoc1() => Navigation.NavigateToViewModel<TabDoc1ViewModel>(this);
    }

    public class TabDoc1ViewModel
    {
        public string Title => "Doc1";
    }

}
