using System.Collections.Generic;
using System.Linq;
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

        private INavigationService Navigation { get; }
        public MainViewModel(INavigationService navigation)
        {
            Navigation = navigation;
            NavigateToSecondPageCommand = new RelayCommand(NavigateToSecondPage);
        }


        public ICommand NavigateToSecondPageCommand { get; }

        private void NavigateToSecondPage() => Navigation.NavigateToViewModel<SecondViewModel>(this, new Widget());

    }

    public class SecondViewModel:INavigationStart, INavigationStop
    {
        public string Title => "Second - " + Data;
        private Widget Data;
        public SecondViewModel(INavigationService nav, Widget data)
        {
            Data = data;
        }

        public async Task Start(NavigationContext context, bool create)
        {
            await Task.Delay(10000);
        }

        public async Task Stop(NavigationContext context, bool cleanup)
        {
            if (context.Path == typeof(ThirdPage).Name &&
                !((context.Data as IDictionary<string,object>)?.Any()??false))
            {
                context.Cancel();
            }
        }
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
