using System;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Uno.Extensions.RuntimeTests.Droid;
[Activity(
    MainLauncher = true,
    ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
    WindowSoftInputMode = SoftInput.AdjustNothing | SoftInput.StateHidden
)]
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
{
    /// <summary>
    /// Variables the CI script may pass as intent extras. Android has no way to set process
    /// environment variables for an app launched via <c>adb shell am start</c>, so the script sends
    /// them as extras and this bridges them into the managed environment before the app starts.
    /// </summary>
    /// <remarks>
    /// Only these exact keys are copied. This app is a test harness that is never shipped to
    /// users, but honouring arbitrary intent extras as environment variables would still be an
    /// injection vector, so the allow-list stays.
    /// </remarks>
    private static readonly string[] RuntimeTestVariables =
    {
        "UITEST_RUNTIME_AUTOSTART_RESULT_FILE",
        "UITEST_RUNTIME_TESTS_FILTER",
    };

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Must run before base.OnCreate: that is what starts the Uno application, and
        // MobileRuntimeTestsAutostart reads these during OnLaunched.
        var extras = Intent?.Extras;
        if (extras is not null)
        {
            foreach (var key in extras.KeySet() ?? Array.Empty<string>())
            {
                if (RuntimeTestVariables.Contains(key))
                {
                    // Fully qualified: Android.OS.Environment is in scope here too.
                    System.Environment.SetEnvironmentVariable(key, extras.GetString(key));
                }
            }
        }

        base.OnCreate(savedInstanceState);
    }
}
