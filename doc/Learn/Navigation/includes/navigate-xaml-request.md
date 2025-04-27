---
uid: Uno.Extensions.Navigation.Xaml.Request
---

<!-- markdownlint-disable MD041 -->

Navigation can be defined in XAML by placing the `Navigation.Request` attached property on a specific XAML element. The string value specified in the `Navigation.Request` is the route to be navigated to.
Depending on the type of the XAML element, the `Navigation.Request` property will attach to an appropriate event in order to trigger navigation. For example, on a `Button`, the `Click` event will be used to trigger navigation, where the `SelectionChanged` event on a `ListView` is used. If you place a `Navigation.Request` property on a static element, such as a `Border`, `Image`, or `TextBlock`, the `Tapped` event will be used to trigger navigation.

- Add a new `Page` to navigate to and name it `SamplePage.xaml`
