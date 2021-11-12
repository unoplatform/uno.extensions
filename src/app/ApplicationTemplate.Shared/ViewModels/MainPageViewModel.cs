using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Uno.Extensions;
using Uno.Extensions.Configuration;
using Uno.Extensions.Localization;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Messages;

namespace ApplicationTemplate.ViewModels
{
    public class MainPageViewModel : ObservableValidator
    {
        private readonly IWritableOptions<LocalizationSettings> _localizationSettings;
        public MainPageViewModel(
            IStringLocalizer localizer,
            IWritableOptions<LocalizationSettings> localizationSettings,
            IOptions<CustomIntroduction> settings,
            IRouteMessenger messenger)
        {
            _localizationSettings = localizationSettings;
            Introduction = settings?.Value?.Introduction;
            ResourcedIntroduction = localizer.GetString("HelloWorld");
            Messenger = messenger;
            GoSecondCommand = new RelayCommand(GoSecond);
            ToggleLocalizationCommand = new RelayCommand(ToggleLocalization);
        }

        public string Introduction { get; }
        public string ResourcedIntroduction { get; }

        public ICommand ToggleLocalizationCommand { get; }
        public ICommand GoSecondCommand { get; }

        private IRouteMessenger Messenger { get; }

        public void ToggleLocalization()
        {
            _localizationSettings.Update(settings =>
            {
                settings.CurrentCulture = settings.Cultures[
                    (
                        (
                            string.IsNullOrWhiteSpace(settings.CurrentCulture) ?
                                0 :
                                settings.Cultures.IndexOf(settings.CurrentCulture)
                        ) + 1
                    ) % settings.Cultures.Length];
            });
        }

        public void GoSecond()
        {
            Messenger.Send(new RoutingMessage(this, typeof(SecondPageViewModel).AsRoute()));
        }
    }
}
