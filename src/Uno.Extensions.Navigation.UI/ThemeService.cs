using System;
using System.Collections.Generic;
using System.Text;
using Uno.Toolkit.UI;
using Windows.Storage;

namespace Uno.Extensions.Navigation.UI
{
	public class ThemeService : IThemeService
	{
		private readonly Window _window;
		private readonly IDispatcher _dispatcher;
		private ApplicationTheme _storedTheme;
		private ApplicationDataContainer _localSettings;
		private const string _storedThemeKey = "localStorageThemeKey";
		private const string _useSystemThemeKey = "localStorageSystemThemeKey";

		public ThemeService(Window window, IDispatcher dispatcher)
		{
			_window = window;
			_dispatcher = dispatcher;
			_storedTheme = SystemThemeHelper.GetCurrentOsTheme();
			_localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
		}

		private bool CheckForStoredTheme()
		{
			try
			{
				var storedValue = _localSettings.Values[_storedThemeKey] as string;
				if (Enum.TryParse(storedValue, out ApplicationTheme storedTheme)) _storedTheme = storedTheme;
				return true;
			} catch { }

			return false;
		}

		/// <summary>
		/// Get if the application is currently in dark mode.
		/// </summary>
		public bool IsDarkMode => SystemThemeHelper.IsRootInDarkMode(_window.Content.XamlRoot!);

		/// <summary>
		/// Gets/Sets if SystemTheme should be used when calling SetThemeAsync()
		/// </summary>
		public bool UseSystemTheme
		{
			get
			{
				var storedValue = _localSettings.Values[_useSystemThemeKey] as string;
				return !string.IsNullOrEmpty(storedValue) ? bool.Parse(storedValue) : false;
			}
			set
			{
				if (value) _localSettings.Values[_useSystemThemeKey] = "true";
				else _localSettings.Values[_useSystemThemeKey] = "false";
			}
		}

		/// <summary>
		/// Sets the theme for the provided XamlRoot
		/// </summary>
		/// <param name="darkMode">Desired mode</param>
		public async Task SetThemeAsync(bool darkMode)
		{
			await _dispatcher.ExecuteAsync(() =>
			{
				SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, darkMode);
				if (darkMode) _storedTheme = ApplicationTheme.Dark;
				else _storedTheme = ApplicationTheme.Light;

				//Override previously saved values
				UseSystemTheme = false;
				_localSettings.Values[_storedThemeKey] = _storedTheme.ToString();
			});
		}

		/// <summary>
		/// Sets the system theme for the provided XamlRoot using previously saved theme value (if any)
		/// or the default system theme if property UseSystemTheme is set to true.
		/// </summary>
		public async Task SetThemeAsync()
		{
			if (!UseSystemTheme)
			{
				//Set previously saved theme
				if (CheckForStoredTheme())
				{
					await _dispatcher.ExecuteAsync(() =>
					{
						SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, _storedTheme == ApplicationTheme.Dark);
						_localSettings.Values[_storedThemeKey] = _storedTheme.ToString();
					});
				}
				else
				{
					//No Theme found. Save current one
					_storedTheme = SystemThemeHelper.GetCurrentOsTheme();
					_localSettings.Values[_storedThemeKey] = _storedTheme;

				}
			}
			else
			{
				//Set System theme
				_storedTheme = SystemThemeHelper.GetCurrentOsTheme();
				SystemThemeHelper.SetRootTheme(_window.Content.XamlRoot, _storedTheme == ApplicationTheme.Dark);
			}
		}
	}
}
