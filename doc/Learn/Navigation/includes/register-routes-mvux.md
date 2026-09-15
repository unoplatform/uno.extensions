---
uid: Uno.Extensions.Navigation.RegisterRoutes-Mvux
---
<!-- markdownlint-disable MD041-->

```csharp
private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
{
    views.Register(
        new ViewMap(ViewModel: typeof(ShellModel)),
        new ViewMap<MainPage, MainModel>(),
        new DataViewMap<SecondPage, SecondModel, Entity>(),
        new ViewMap<SamplePage, SampleModel>()
    );

    routes.Register(
        new RouteMap("", View: views.FindByViewModel<ShellModel>(),
            Nested:
            [
                new ("Main", View: views.FindByViewModel<MainModel>()),
                new ("Second", View: views.FindByViewModel<SecondModel>()),
                new ("Sample", View: views.FindByViewModel<SampleModel>()),
            ]
        )
    );
}
 ```
