---
uid: Uno.Extensions.Navigation.RegisterRoutes-Mvvm
---
<!-- markdownlint-disable MD041-->

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellViewModel)),
        new ViewMap<MainPage, MainViewModel>(),
        new DataViewMap<SecondPage, SecondViewModel, Entity>(),
        new ViewMap<SamplePage, SampleViewModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
            Nested:
            [
                new ("Main", View: views.FindByViewModel<MainViewModel>()),
                new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                new ("Sample", View: views.FindByViewModel<SampleViewModel>()),
            ]
        )
    );
}
 ```
