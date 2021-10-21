﻿using System;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SecondPage : IInjectable<INavigator>, IInjectable<IRouteMappings>
    {
        private INavigator Navigation { get; set; }

        private IRouteMappings Mappings { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public void Inject(IRouteMappings mappings)
        {
            Mappings = mappings;
        }

        public SecondPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ParametersText.Text = e.Parameter.ParseParameter();
        }

        private void GoBackFrameClick(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
        private void GoBackNavigationRequestClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri("../-", UriKind.Relative).AsRoute()));
        }

        private void GoBackNavigateToPreviousViewAsyncClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToPreviousViewAsync(this, data: new Widget());
        }

        private async void NextPageNavigateToViewAsyncRequestDataClick(object sender, RoutedEventArgs e)
        {
            var response = await Navigation.NavigateToViewForResultAsync<ThirdPage, Widget>(Mappings, this);
            if (response?.Result != null)
            {
                await response.Result;
            }
        }

        private void NextPageNavigateToViewAsyncWithDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToViewAsync<ThirdPage>(Mappings, this, data: new Widget());
        }

        private void NextPageNavigateToViewAsyncWithQueryAndDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri(typeof(ThirdPage).Name + "?arg1=val1&arg2=val2", UriKind.Relative).AsRoute(new Widget())));
        }
    }

    public class Widget
    {
        public string Title { get; } = "This is a widget to test sending data in navigation - " + DateTimeOffset.Now.ToString();

        public override string ToString()
        {
            return $"Widget - {Title}";
        }
    }
}
