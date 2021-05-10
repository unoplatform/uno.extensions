using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
	public class DiagnosticsPageViewModel : ViewModel
	{
		public IViewModel SummaryDiagnostics => this.GetChild<SummaryDiagnosticsViewModel>();

		public IViewModel ExceptionDiagnostics => this.GetChild<ExceptionsDiagnosticsViewModel>();

		public IViewModel CultureDiagnostics => this.GetChild<CultureDiagnosticsViewModel>();

		public IViewModel LoggersDiagnostics => this.GetChild<LoggersDiagnosticsViewModel>();

		public IViewModel SettingsDiagnostics => this.GetChild<SettingsDiagnosticsViewModel>();

		public IDynamicCommand NavigateToEnvironmentPickerPage => this.GetCommandFromTask(async ct =>
		{
			await this.GetService<IStackNavigator>().Navigate(ct, () => new EnvironmentPickerPageViewModel(CurrentEnvironment));
		});

		public string CurrentEnvironment
		{
			get => this.GetFromTask(GetCurrentEnvironment);
			set => this.Set(value);
		}

		private Task<string> GetCurrentEnvironment(CancellationToken ct)
		{
			return Task.FromResult(AppSettingsConfiguration.AppEnvironment.GetCurrent());
		}
	}
}
