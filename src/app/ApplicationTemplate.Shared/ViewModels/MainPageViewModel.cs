using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;

namespace ApplicationTemplate.ViewModels
{
    public class MainPageViewModel : ObservableValidator
    {
        public MainPageViewModel(
            IOptions<CustomIntroduction> settings,
            IRouteMessenger messenger)
        {
            Introduction = settings?.Value?.Introduction;
            Messenger = messenger;
            GoSecondCommand = new RelayCommand(GoSecond);
        }

        public string Introduction { get; }

        public ICommand GoSecondCommand { get; }

        private IRouteMessenger Messenger { get; }

        public void GoSecond()
        {
            Messenger.Send(new RoutingMessage(this, typeof(SecondPageViewModel).AsRoute()));
        }
    }
}
