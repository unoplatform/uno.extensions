using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Configuration;
using Uno.Toolkit.UI;
using Windows.Storage;

namespace Uno.Extensions.Navigation.UI
{
	public class ThemeService : IThemeService
	{
		private readonly Window _window;
		private readonly IDispatcher _dispatcher;
		private readonly IWritableOptions<ThemeSettings> _writeSettings;

		/// <inheritdoc/>
		public event EventHandler<DesiredTheme>? DesiredThemeChanged;

		public ThemeService(Window window, IDispatcher dispatcher, IWritableOptions<ThemeSettings> writeSettings)
		{
			_window = window;
			_dispatcher = dispatcher;
			_writeSettings = writeSettings;
		}

		/// <inheritdoc/>
		public bool IsDark => SystemThemeHelper.IsRootInDarkMode(_window.Content.XamlRoot!);

		/// <inheritdoc/>
		public DesiredTheme Theme => GetSavedTheme();

		/// <inheritdoc/>
		public async Task SetThemeAsync(DesiredTheme theme)
		{
			if (theme != DesiredTheme.System)
			{
				await _dispatcher.ExecuteAsync(async () =>
				{
					SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, theme == DesiredTheme.Dark);
				});

			}
			else
			{
				//Set System theme
				var systemTheme = SystemThemeHelper.GetCurrentOsTheme();
				SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, systemTheme == ApplicationTheme.Dark);
			}

			await SaveDesiredTheme(theme);
			DesiredThemeChanged?.Invoke(this, theme);
		}

		private async Task SaveDesiredTheme(DesiredTheme theme)
		{
			try
			{
				await _writeSettings.UpdateAsync(themeSetting => themeSetting with { CurrentTheme = theme });
			}
			catch { }
		}

		private DesiredTheme GetSavedTheme()
		{
			try
			{
				return _writeSettings.Value.CurrentTheme;
			}
			catch { }

			return DesiredTheme.System;
		}
	}
}
