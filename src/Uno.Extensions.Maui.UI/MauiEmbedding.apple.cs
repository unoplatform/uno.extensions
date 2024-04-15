using UIKit;
using Uno.Extensions.Maui.Platform;
using Microsoft.Maui;
using Windows.UI.Core;
using Uno.UI.Xaml;

namespace Uno.Extensions.Maui;

partial class MauiEmbedding
{
    // NOTE: This is meant to help initialize MauiEmbedding similar to MauiUIApplicationDelegate
    // https://github.com/dotnet/maui/blob/ace9fe5e7d8d9bd16a2ae0b2fe2b888ad681433e/src/Core/src/Platform/iOS/MauiUIApplicationDelegate.cs#L36-L70
    private static MauiAppBuilder RegisterPlatformServices(this MauiAppBuilder builder, Application app)
    {
        builder.Services.AddTransient<UIWindow>(sp =>
        {
            var window = sp.GetRequiredService<Microsoft.UI.Xaml.Window>();
            var nativeWindow = WindowHelper.GetNativeWindow(window);
            return nativeWindow is UIWindow uiWindow ? uiWindow : throw new InvalidOperationException("Unable to locate the Native UIWindow");
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
