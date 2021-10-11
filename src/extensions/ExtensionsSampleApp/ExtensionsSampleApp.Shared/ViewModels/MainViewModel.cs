using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtensionsSampleApp.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Logging;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;

namespace ExtensionsSampleApp.ViewModels
{
    public class MainViewModel : ObservableObject, IViewModelStart
    {
        public string Title => "Main";

        private INavigationService Navigation { get; }
        public MainViewModel(INavigationService navigation)
        {
            Navigation = navigation;
            NavigateToSecondPageCommand = new RelayCommand(NavigateToSecondPage);
        }
        public async Task Start(NavigationRequest request)
        {
        }

        public ICommand NavigateToSecondPageCommand { get; }

        private void NavigateToSecondPage() => Navigation.NavigateToViewModelAsync<SecondViewModel>(this, data: new Widget());

        public List<string> PickerItems { get; } = new List<string>() { "One", "Two", "Three" };

    }

    public class SecondViewModel : IViewModelStart, IViewModelStop
    {
        public string Title => "Second - " + Data;
        private Widget Data;
        private ILogger Logger { get; }
        public SecondViewModel(ILogger<SecondViewModel> logger, INavigationService nav, Widget data)
        {
            Logger = logger;
            Data = data;
        }

        public async Task Start(NavigationRequest request)
        {
            Logger.LazyLogTrace(() => "Starting view model (delay 5s)");
            await Task.Delay(5000);
            Logger.LazyLogTrace(() => "View model started (5s delay completed)");
        }

        public async Task<bool> Stop(NavigationRequest request)
        {
            if ((request.Segments.Base == typeof(ThirdPage).Name ||
                request.Segments.Base == typeof(ThirdPage).Name.Replace("Page",""))&&
                !((request.Parse().Parameters as IDictionary<string, object>)?.Any() ?? false))
            {
                return false;
            }
            return true;
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
            NavigateToThirdPageCommand = new RelayCommand(NavigateToThirdPage);
        }



        public ICommand NavigateToDoc1Command { get; }
        public ICommand NavigateToThirdPageCommand { get; }

        private void NavigateToDoc1() => Navigation.NavigateToViewModelAsync<TabDoc1ViewModel>(this);

        private void NavigateToThirdPage() => Navigation.NavigateToViewModelAsync<ThirdViewModel>(this, RouteConstants.RelativePath.Parent(1));

    }

    public class TabDoc1ViewModel
    {
        public string Title => "Doc1";
    }

    public class TweetsViewModel
    {
        public TweetsViewModel()
        {

        }

        public IList<Tweet> Tweets { get; } = new List<Tweet>()
        {
            new Tweet() {Author= "Fred", Text="First tweet"},
            new Tweet() {Author= "Fred2", Text="Second tweet"},
            new Tweet() {Author= "Fred3", Text="Third tweet"},
            new Tweet() {Author= "Fred4", Text="Fourth tweet"},
            new Tweet() {Author= "Fred5", Text="Fifth tweet"},
            new Tweet() {Author= "Fred5", Text="Sixth tweet"},
            new Tweet() {Author= "Fred6", Text="Seventh tweet"},

        };
    }

    public class NotificationsViewModel
    {
        public IList<Tweet> Notifications { get; } = new List<Tweet>()
        {
            new Tweet() {Author= "Fred", Text="First tweet"},
            new Tweet() {Author= "Fred2", Text="Second tweet"},
            new Tweet() {Author= "Fred3", Text="Third tweet"},
            new Tweet() {Author= "Fred4", Text="Fourth tweet"},
            new Tweet() {Author= "Fred5", Text="Fifth tweet"},
            new Tweet() {Author= "Fred5", Text="Sixth tweet"},
            new Tweet() {Author= "Fred6", Text="Seventh tweet"},

        };
    }

    public class TweetDetailsViewModel : ObservableObject, IViewModelStart
    {
        private Tweet tweet;
        public Tweet Tweet { get => tweet; set => SetProperty(ref tweet, value); }
        public TweetDetailsViewModel(Tweet tweet)
        {
            Tweet = tweet;
        }

        public async Task Start(NavigationRequest context)
        {
            if (Tweet is null)
            {
                Tweet = new Tweet { Id = int.Parse(context.Parse().Parameters["tweetid"] + ""), Author = "Ned", Text = "Tweet loaded on start" };
            }
        }
    }

    public class Content2ViewModel
    {
        public string Title => "Content2 - " + DateTime.Now.ToString();
    }


    public class Tweet
    {
        public static int NextId = 1;
        public int Id { get; set; } = NextId++;
        public string Author { get; set; }
        public string Text { get; set; }
    }

}
