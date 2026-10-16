using System;
using Android.App;
using Android.Runtime;
using Uno.UI.Hosting;

namespace Uno.Extensions.RuntimeTests.Droid;

[global::Android.App.ApplicationAttribute(
    Label = "@string/ApplicationName",
    Icon = "@mipmap/icon",
    LargeHeap = true,
    HardwareAccelerated = true,
    Theme = "@style/AppTheme"
)]
public class Application : Uno.UI.Runtime.Android.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override UnoPlatformHost CreateHost()
    {
        App.InitializeLogging();

        return UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAndroid()
            .Build();
    }
}
