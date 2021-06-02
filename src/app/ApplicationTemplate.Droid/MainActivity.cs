using Android.App;
using Android.Views;

namespace ApplicationTemplate.Droid
{
    [Activity(
            MainLauncher = true,
            ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
            WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden
        )]
    public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
    {
    }
}
