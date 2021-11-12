using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;

namespace ApplicationTemplate.ViewModels
{
    public class SecondPageViewModel : ObservableObject
    {
        public SecondPageViewModel(
            IRouteMessenger messenger)
        {
            Messenger = messenger;
            GoBackCommand = new RelayCommand(GoBack);
        }

        public string Title { get; } = "Page 2";

        public ICommand GoBackCommand { get; }

        private IRouteMessenger Messenger { get; }

        public void GoBack()
        {
            Messenger.Send(new CloseMessage(this));
        }
    }
}
