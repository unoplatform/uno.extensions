using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace ExtensionsSampleApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ThirdPage : IInjectable<INavigator>
    {
        private INavigator Navigation { get; set; }

        public void Inject(INavigator entity)
        {
            Navigation = entity;
        }

        public ThirdPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ParametersText.Text = e.Parameter.ParseParameter();
        }

        private void NextPagePreviousViewWithDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateToPreviousViewAsync(this, data: new Widget());
        }

        private void NextPagePreviousViewWithArgsAndDataClick(object sender, RoutedEventArgs e)
        {
            Navigation.NavigateAsync(new NavigationRequest(sender, new Uri("../<?arg1=val1&arg2=val2", UriKind.Relative).AsRoute(new Widget())));
        }
    }

    public static class PageHelpers
    {
        public static string ParseParameter(this object parameter)
        {
            if (parameter is IDictionary<string, object> argsDict)
            {
                return string.Join(Environment.NewLine, from p in argsDict
                                                        select $"key '{p.Key}' val '{p.Value}'");
            }
            return parameter?.ToString() ?? string.Empty;
        }
    }
}
