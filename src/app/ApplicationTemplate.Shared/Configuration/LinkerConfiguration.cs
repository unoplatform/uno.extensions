using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationTemplate
{
    /// <summary>
    /// This class is used for linker configuration.
    /// - Adds linker exceptions.
    /// </summary>
    public static class LinkerConfiguration
    {
        public static void LinkMe()
        {
#if (IncludeFirebaseAnalytics)
//-:cnd:noEmit
#if __IOS__
//+:cnd:noEmit
            var a = Firebase.Core.Configuration.SharedInstance;
            var b = Firebase.RemoteConfig.RemoteConfig.SharedInstance.ConfigSettings;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
#endif
        }
    }
}
