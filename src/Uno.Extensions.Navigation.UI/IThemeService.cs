using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Navigation.UI
{
	public interface IThemeService
	{
		bool IsDarkMode { get; }
		Task SetThemeAsync(bool darkMode);
		Task SetThemeAsync();
	}
}
