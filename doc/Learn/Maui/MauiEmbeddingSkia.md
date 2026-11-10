---
uid: Uno.Extensions.Maui.Embedding
---
# .NET MAUI Embedding on Skia desktop and WebAssembly

`Uno.Maui.Embedding` shows .NET MAUI views, including third-party MAUI controls, inside an Uno Platform app on the Skia desktop and WebAssembly targets. The app stays an Uno app, with Uno windows, pages and XAML. A `MauiHost` element in the visual tree shows a MAUI view, drawn by MAUI's own WinUI handlers through Skia.

It is selected by the `MauiEmbedding` UnoFeature. The native embedding of Uno.Extensions, which runs on Android, iOS and Windows App SDK, is the `MauiNativeEmbedding` UnoFeature, described in the [overview](xref:Uno.Extensions.Maui.Overview).

> [!IMPORTANT]
> `Uno.Maui.Embedding` is an experimental preview. Its API, its MSBuild properties and the MAUI versions it accepts can change in any `0.x` version.

## Status

- **Run on:** Skia desktop on macOS, Windows and Linux, and WebAssembly in Chromium-based browsers.
- **Not run:** embedding on Android and iOS, and accessibility. On these targets, use `MauiNativeEmbedding`.
- **MAUI version:** `Microsoft.Maui.Controls` 10.0.101 only. Another 10.0 patch release builds with warning `UMH001`. Another minor or major version stops the build unless `UnoMauiAllowUnsupportedVersion` is `true`.
- **Uno Platform:** Uno.WinUI `7.0.29` or later. The `MauiEmbedding` and `MauiNativeEmbedding` UnoFeatures are in Uno.Sdk `7.0.0-dev.189` and later.

## Packages

| Package | What it does |
| --- | --- |
| [Uno.Maui.Embedding](https://www.nuget.org/packages/Uno.Maui.Embedding) | `MauiHost` and `UseMauiEmbedding`: shows MAUI views in an Uno app. |
| [Uno.Maui.Handlers](https://www.nuget.org/packages/Uno.Maui.Handlers) | Runs MAUI's own WinUI handlers on Uno. When the app builds, it rewrites the `net10.0-windows` assemblies of Microsoft's `Microsoft.Maui.*` packages and of the MAUI libraries the app references. The app's own code is not changed. |
| [Uno.WinUI.Retarget](https://www.nuget.org/packages/Uno.WinUI.Retarget) | The generic layer under Uno.Maui.Handlers. When the app builds, it rewrites the Windows build of a Windows App SDK (WinUI 3) package so that it binds to Uno's `Microsoft.UI.Xaml`. The packages on disk are not changed, and packages that ship an Uno build are left alone. It is also the `Retarget` UnoFeature. |

The three packages share one version. Uno.Maui.Embedding depends on exactly the same version of Uno.Maui.Handlers, which depends on exactly the same version of Uno.WinUI.Retarget. Every change to the packages publishes the next `0.1.0-dev.<n>` on nuget.org.

## Get Started

### UnoFeatures

In an Uno.Sdk project, add `MauiEmbedding` to `UnoFeatures`:

```diff
<UnoFeatures>
   ...
+  MauiEmbedding;
</UnoFeatures>
```

The feature references Uno.Maui.Embedding and `Microsoft.Maui.Controls` 10.0.101, and turns on the `Retarget` feature. `UnoMauiVersion` overrides the version of the Uno.Maui packages, `UnoRetargetVersion` the version of Uno.WinUI.Retarget, and `MauiVersion` the version of MAUI. Set `UnoMauiVersion` and `UnoRetargetVersion` to the same version, since the packages depend on each other's exact version.

The first Uno.Sdk builds with the feature, from `7.0.0-dev.189`, reference `0.1.0-dev.1`, which doesn't have the `Uno.Extensions.Maui` API (see [Coming from MauiNativeEmbedding](#coming-from-mauinativeembedding)). If the build reports `UXAML0001` for `MauiHost`, set both properties in `Directory.Build.props`:

```xml
<UnoMauiVersion>0.1.0-dev.4</UnoMauiVersion>
<UnoRetargetVersion>0.1.0-dev.4</UnoRetargetVersion>
```

Before Uno.Sdk 7.0, `MauiEmbedding` was the native embedding of Uno.Extensions, which is now named `MauiNativeEmbedding`. On an Android, iOS, Mac Catalyst or Windows App SDK target, a project that lists `MauiEmbedding` gets warning [UNOB0036](xref:Build.Solution.error-codes#unob0036-mauiembedding-now-selects-unomauiembedding). If the project needs MAUI views on these targets, replace `MauiEmbedding` with `MauiNativeEmbedding`.

### Package references

Without Uno.Sdk, reference the packages in the Uno head project:

```xml
<PackageReference Include="Uno.Maui.Embedding" Version="0.1.0-dev.*" />
<PackageReference Include="Microsoft.Maui.Controls" Version="10.0.101" />
```

### The MAUI application

Describe the MAUI side with a MAUI application type. It has no pages: the Uno app owns the window.

```csharp
public sealed class MauiPart : Microsoft.Maui.Controls.Application { }
```

To use MAUI XAML, put this type and the MAUI views in a .NET MAUI class library, as the Uno template does (see [MAUI views in a MAUI class library](#maui-views-in-a-maui-class-library)).

### UseMauiEmbedding

Call `UseMauiEmbedding` for each window in `OnLaunched`, before its first `MauiHost` loads. It registers MAUI's XAML resources, builds the MAUI app and creates the window's MAUI context. Register a library's handlers in the optional callback.

**Without** Uno.Extensions:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    MainWindow = new Window();
    this.UseMauiEmbedding<MauiPart>(MainWindow, maui => maui
        .UseTelerik()                  // the libraries' registrations, for example
        .ConfigureSyncfusionCore());   // UseMauiCommunityToolkit() or ConfigureSyncfusionCore()
    ...
}
```

**With** Uno.Extensions, call it on the application builder:

```csharp
var builder = this.CreateBuilder(args)
    .UseMauiEmbedding<MauiControls.App>(maui => maui
        .UseMauiControls())
    .Configure(host => host
        ...
```

The MAUI app and the Uno host then share one service provider: a service the host registers resolves from the MAUI app.

A library's controls render only when its handlers are registered in the callback. Otherwise MAUI throws `HandlerNotFoundException`, and the host shows the message in place of the control. `UnoMauiContext.GetForWindow(window)` returns the window's context, with the `MauiApp` and its services. If the app uses Uno's Fluent styles, merge `XamlControlsResources` into the application resources before `UseMauiEmbedding`.

### MauiHost

`MauiHost` takes the type of a MAUI view in `Source` and creates the view when the host loads:

```xml
<Page x:Class="MyApp.MainPage"
      xmlns:embed="using:Uno.Extensions.Maui"
      xmlns:controls="using:MyApp.MauiControls"
      ...>
    <embed:MauiHost Source="controls:EmbeddedControl" />
</Page>
```

```csharp
var host = new MauiHost { Source = typeof(EmbeddedControl) };
```

The type needs a public constructor. MAUI's services supply its constructor arguments. The host's `DataContext` is the view's `BindingContext`, so a MAUI binding in the view reads the Uno page's view model.

`MauiHost` is a `ContentControl`. `VisualElement` and `VisualElementChanged` give the MAUI element once it is created, and `RealizedView` and `PlatformView` give the MAUI view and its native element. `ReleaseOnUnloaded` (default `true`) frees the handlers when the host leaves the visual tree. A host that is removed and added again loses native state, such as the scroll position.

## MAUI views in a MAUI class library

Put the MAUI views in a .NET MAUI class library and reference it from the Uno app. Each XAML file is compiled by its own compiler: the Uno pages by Uno's, the library's views by MAUI's. The editor treats the library as a MAUI library.

- The Uno template's MAUI class library works unchanged. The Uno desktop and WebAssembly targets use its `net10.0` build. The app's `Microsoft.Maui.Controls` version is the one that runs.
- Code under `#if WINDOWS` in the library is not compiled. At run time, `OnPlatform` and `DeviceInfo.Platform` report `WinUI`, as on Windows.
- Application resources belong to the MAUI application. Give the library's `App.xaml` the dictionaries the views look up, and pass that type to `UseMauiEmbedding`. Uno's application resources are separate.
- In Release, MAUI compiles the library's XAML (XamlC) and reports a wrong property (`XC0009`) or a binding path that `x:DataType` does not have (`XC0045`). In Debug, `<MauiXamlInflator>SourceGen</MauiXamlInflator>` in the library reports them too.
- XAML Hot Reload of the library's XAML was not run.

## MAUI controls declared in Uno XAML

For a small view, declare the MAUI control as the content of the host. `View` is the host's content property:

```xml
<embed:MauiHost xmlns:embed="using:Uno.Extensions.Maui" xmlns:m="using:Microsoft.Maui.Controls">
    <m:Button Text="A MAUI button" />
</embed:MauiHost>
```

Uno's XAML compiler sets strings, numbers, booleans and enums on a MAUI object, but it does not run MAUI's type converters. A property that needs one, such as a `Thickness`, `LayoutOptions`, `Color` or `RowDefinitionCollection`, fails to build with `UXAML0001`. Write those with `{embed:Convert Value=...}`, for example `Padding="{embed:Convert Value=8}"`. A wrong `Value` is not a build error: the page fails to load at run time.

Other limits of this form:

- One MAUI view per host. Wrap several in a MAUI layout.
- `{Binding}`, `{x:Bind}` and `{StaticResource}` on a MAUI property fail to build.
- An event handler has MAUI's signature, for example `(object? sender, EventArgs e)`.
- The view is created without MAUI's services, so it can't take constructor arguments.

A view that needs any of these is a class or MAUI XAML in a MAUI class library, shown with `Source`.

## Coming from MauiNativeEmbedding

`Uno.Maui.Embedding` keeps the API of `Uno.Extensions.Maui.WinUI`:

- `MauiHost` and `UseMauiEmbedding` are in `Uno.Extensions.Maui`, with the same members and overloads, including `UseMauiEmbedding` on Uno.Extensions' `IApplicationBuilder` and `IHostBuilder`.
- `VisualElementChangedEventArgs`, `MauiEmbeddingException` and `MauiEmbeddingInitializationException` are kept.
- With implicit usings, the package adds the global usings `Uno.Extensions.Maui` and `Uno.Extensions.Maui.Platform`.
- On the desktop and WebAssembly targets, the package defines `MAUI_EMBEDDING` and the `maui` conditional XAML namespace, which the Uno template uses.

So an app created from the Uno template with MAUI embedding builds and runs with `MauiEmbedding` on desktop and WebAssembly without changes to its code.

What differs:

- The targets. `Uno.Maui.Embedding` does not show MAUI views on Android, iOS or Windows App SDK, and doesn't define `MAUI_EMBEDDING` there.
- `Uno.Extensions.Maui.WinUI.Markup` (C# Markup) has no counterpart.
- `MauiEmbeddingException` and `MauiEmbeddingInitializationException` exist but are not thrown.

The namespace `Uno.Extensions.Maui` is in `Uno.Maui.Embedding` `0.1.0-dev.4` and later. In `0.1.0-dev.1` and `0.1.0-dev.2` the types are in `Uno.Maui.Embedding`, and an app written for `MauiNativeEmbedding` fails to build with `UXAML0001` (`MauiHost` not found) and `CS1061` (`UseMauiEmbedding`).

## Existing Uno projects

- **Usings and MAUI XAML.** `Microsoft.Maui.Controls` adds MAUI's global usings, which make `Window`, `Grid` and `Thickness` ambiguous with Uno's, and treats every `*.xaml` file as MAUI XAML. In a project that is not a MAUI project, the package turns both off. Add the MAUI usings a file needs, such as `Microsoft.Maui.Hosting`. `UnoMauiKeepMauiProjectDefaults=true` keeps MAUI's defaults.
- **Libraries with only platform builds (`NU1202`).** A MAUI library that ships `net10.0-android`, `net10.0-ios` and `net10.0-windows10.0.19041.0` builds, but no `net10.0` one, has no asset for an Uno desktop or WebAssembly target. The `Retarget` UnoFeature adds the Windows target frameworks to `AssetTargetFallback`. Without Uno.Sdk, add them yourself.
- **Warnings.** The rewritten assemblies carry Windows platform attributes, so the package adds `CA1416` to `NoWarn`.

## Light and dark themes

MAUI's own controls follow the application theme: a `MauiHost` tells MAUI when its theme changes, and MAUI re-evaluates `AppThemeBinding`. A vendor's controls take their colors from the vendor's theme resources, which stay light unless the app selects the dark ones, as in a MAUI app:

- Keep a `RequestedThemeChanged` handler in a `static` field. MAUI holds the event's handlers weakly, so a lambda that captures locals is collected and never runs.
- **Syncfusion:** merge a `SyncfusionThemeResourceDictionary` into the MAUI application's resources, and set its `VisualTheme` to `SfVisuals.MaterialLight` or `SfVisuals.MaterialDark` when the theme changes.
- **Telerik:** set `<UseTelerikTheming>true</UseTelerikTheming>`, merge `TelerikThemeResources` into the MAUI application's resources, and set its `Theme` when the theme changes. `UnoMauiPatchGeneratedTheming=false` turns off the package's patch of Telerik's generated theming code.
- Libraries that draw with fixed colors stay as they are on every MAUI platform.

## Limits

- One MAUI application per process. A second window was not verified.
- `Shell`, `NavigationPage`, `FlyoutPage` and `TabbedPage` can be the view of a host. Give the host a definite `Height`. Modal pages are not supported.
- The host arranges its view to its own size. Give the host or the view a width and height when its parent doesn't.
- Embedding on Android and iOS through Skia is not done.
