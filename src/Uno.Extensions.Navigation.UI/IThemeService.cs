using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Navigation.UI
{
	public interface IThemeService
	{
		/// <summary>
		/// Get if the application is currently in dark mode.
		/// </summary>
		bool IsDark { get; }

		/// <summary>
		///  Get the previously saved theme.
		/// </summary>
		DesiredTheme Theme { get; }

		/// <summary>
		/// Sets the system theme for the provided XamlRoot.
		/// </summary>
		Task SetThemeAsync(DesiredTheme theme = DesiredTheme.System);

		/// <summary>
		/// Event that fires up whenever user updates current theme.
		/// </summary>
		event EventHandler<DesiredTheme> DesiredThemeChanged;
	}
}
