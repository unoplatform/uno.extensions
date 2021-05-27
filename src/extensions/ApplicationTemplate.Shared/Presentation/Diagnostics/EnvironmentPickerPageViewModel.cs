using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public class EnvironmentPickerPageViewModel : ViewModel
    {
        private readonly string _currentEnvironment;

        public EnvironmentPickerPageViewModel(string currentEnvironment)
        {
            _currentEnvironment = currentEnvironment;
        }

        public string SelectedEnvironment
        {
            get => this.Get(_currentEnvironment);
            set => this.Set(value);
        }

        public bool RequiresRestart
        {
            get => this.Get<bool>();
            set => this.Set(value);
        }

        public IEnumerable<string> Environments => this.GetFromTask(GetEnvironments);

        public IDynamicCommand SelectEnvironment => this.GetCommandFromTask<string>(async (ct, environment) =>
        {
            if (_currentEnvironment == environment)
            {
                await this.GetService<IStackNavigator>().NavigateBack(ct);

                return;
            }

            AppSettingsConfiguration.AppEnvironment.SetCurrent(environment);

            // TODO #173219 : Disable back button

            RequiresRestart = true;
        });

        private Task<string[]> GetEnvironments(CancellationToken ct)
        {
            return Task.FromResult(AppSettingsConfiguration.AppEnvironment.GetAll());
        }
    }
}
