using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Navigation.UI
{
	public interface IThemeService
	{
		bool IsDark { get; }
		DesiredTheme Theme { get; }
		Task SetThemeAsync(DesiredTheme theme = DesiredTheme.System);

		event EventHandler<DesiredTheme> DesiredThemeChanged;
	}
}
