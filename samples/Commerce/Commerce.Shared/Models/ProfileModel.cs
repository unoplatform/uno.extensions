using System;
using System.Collections.Generic;
using System.Text;
using Uno.Toolkit.UI.Helpers;

namespace Commerce.Models
{
    public record ProfileModel(Profile Profile)
    {
		public string FullName => $"{Profile.FirstName} {Profile.LastName}";

		public string Avatar => Profile.Avatar;

		private bool? _isDarkMode;
		public bool IsDarkMode
		{
			get
			{
				_isDarkMode ??= SystemThemeHelper.IsAppInDarkMode();
				return _isDarkMode.Value;
			}
			set
			{
				SystemThemeHelper.SetApplicationTheme(value);
				_isDarkMode = value;
			}
		}

	}
}
