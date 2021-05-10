using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ApplicationTemplate
{
	public static class DiagnosticsConfiguration
	{
		public static class DiagnosticsOverlay
		{
			public static bool GetIsEnabled()
			{
//-:cnd:noEmit
#if DEBUG
//+:cnd:noEmit
				var defaultValue = true;
//-:cnd:noEmit
#else
//+:cnd:noEmit
				var defaultValue = false;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

				return ConfigurationSettings.GetIsSettingEnabled("diagnostics-overlay", defaultValue);
			}

			public static void SetIsEnabled(bool isEnabled)
			{
				ConfigurationSettings.SetIsSettingEnabled("diagnostics-overlay", isEnabled);
			}
		}
	}
}
