﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExtensionsSampleApp.Views;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.ViewModels;

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
            await Task.Delay(5000);
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

    public class TweetsViewModel
    {
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

    public class TweetDetailsViewModel: ObservableObject, INavigationStart
    {
        private Tweet tweet;
        public Tweet Tweet { get => tweet; set=>SetProperty(ref tweet, value); }
        public TweetDetailsViewModel(Tweet tweet)
        {
            Tweet = tweet;
        }

        public async Task Start(NavigationContext context, bool create)
        {
            if(Tweet is null)
            {
                Tweet = new Tweet { Id = int.Parse(context.Data["tweetid"] + ""), Author = "Ned", Text = "Tweet loaded on start" };
            }
        }
    }

    public class Tweet
    {
        public static int NextId = 1;
        public int Id { get; set; } = NextId++;
        public string Author { get; set; }
        public string Text { get; set; }
    }

}
