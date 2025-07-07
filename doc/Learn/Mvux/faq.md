---
uid: Uno.Extensions.Mvux.FAQ
---

# MVUX FAQ

This document answers frequently asked questions about MVUX

## Can I use MVUX in combination with other MVVM?

Yes, you can use `MVUX` with `MVVM`, although it is not recommended. However, you can make it work by following these steps:

1. It is crucial to avoid having a `ViewModel` and a `Model` with the same name within the same namespace. For instance, having `MainModel` and `MainViewModel` in the same namespace can lead to conflicts, as MVUX will generate its own `MainViewModel` class, which will clash with your existing `MainViewModel`.

2. You need to turn off MVUX generation for your `ViewModel` classes by adding the attribute `[ReactiveBindable(false)]` to them.

   ```csharp
   [ReactiveBindable(false)]
   public class MainViewModel
   {
       // ...
   }
   ```

    > [!NOTE]
    > In MVUX, the `ViewModel` for a `Model` is created when the class name matches the regex "Model$", this means any class that ends with "Model" will be considered a `Model`

3. If you are using the Uno Platform for Navigation, ensure you register your `MVVM` ViewModels in the `App.xaml.cs` file with their corresponding `Page` types.

   ```csharp
    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap<MainPage, MainViewModel>(),
            new DataViewMap<SecondPage, SecondModel, Entity>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault:true),
                    new ("Second", View: views.FindByViewModel<SecondModel>()),
                ]
            )
        );
    }
   ```

> [!IMPORTANT]
> You can implement the `MVVM` pattern manually or enable the `Mvvm` Uno Feature, which adds support for the [CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm) package.
