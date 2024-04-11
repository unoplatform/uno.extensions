namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
    // NOTE: This is meant to help initialize MauiEmbedding similar to MauiUIApplicationDelegate
    // https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/iOS/MauiUIApplicationDelegate.cs#L36-L70
    private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
    {
        builder.Services.AddSingleton<UIWindow>(sp =>
            {
                var window = sp.GetRequiredService<Microsoft.UI.Xaml.Window>();

                // In 5.2 there's a public WindowHelper.GetNativeWindow method but need to call via reflection to maintain support for
                // pre 5.2 versions of Uno
                var nativeWindowProp = window.GetType().GetProperty("NativeWindow", BindingFlags.Instance | BindingFlags.NonPublic);
                if (nativeWindowProp is not null)
                {
                    var nativeWindow = nativeWindowProp.GetValue(window) as UIWindow;
                    return nativeWindow!;
                }

                // The _window field is the only way to grab the underlying UIWindow from inside the CoreWindow
                // https://github.com/unoplatform/uno/blob/34a32058b812a0a08e658eba5e298ea9d258c231/src/Uno.UWP/UI/Core/CoreWindow.iOS.cs#L17
                var internalWindow = typeof(CoreWindow).GetField("_window", BindingFlags.Instance | BindingFlags.NonPublic);
                if (internalWindow is null)
                {
                    throw new MauiEmbeddingException(Properties.Resources.MissingWindowPrivateField);
                }
                var uiwindow = internalWindow?.GetValue(window.CoreWindow) as UIWindow;
                return uiwindow!;
            })
            .AddSingleton<IUIApplicationDelegate>(sp => sp.GetRequiredService<Application>());

        return builder;
    }

    private static void InitializeMauiEmbeddingApp(this MauiApp mauiApp, Application app)
    {
        var rootContext = new MauiContext(mauiApp.Services);
        rootContext.InitializeScopedServices();

        var iApp = mauiApp.Services.GetRequiredService<IApplication>();

        Microsoft.Maui.ApplicationModel.Platform.Init(() => mauiApp.Services.GetRequiredService<UIWindow>().RootViewController!);
        _ = new EmbeddedApplication(mauiApp.Services, iApp);
        app.SetApplicationHandler(iApp, rootContext);
        InitializeApplicationMainPage(iApp);
    }
}
