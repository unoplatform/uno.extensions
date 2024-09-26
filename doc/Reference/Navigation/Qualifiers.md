---
uid: Reference.Navigation.Qualifiers
---

# Navigation Qualifiers

Navigation qualifiers can be utilized to make navigating easier and smoother:

| Qualifier | Description                                     | Example             | Example Usage                                                |
|-----------|-------------------------------------------------|---------------------|--------------------------------------------------------------|
| ""        | Navigate to page in frame or open popup        | "Home"              | Navigate to the `HomePage`                                     |
| /         | Forward request to the root region              | "/"<br>"/Login"     | Navigate to the default route at the root of navigation<br>Navigate to `LoginPage` at the root of navigation |
| ./        | Forward request to child region                 | "./Info/Profile"    | Navigate to the Profile view in the child region named Info |
| !         | Open a dialog or flyout                         | "!Cart"             | Shows the Cart flyout                                        |
| -         | Back (Frame), Close (Dialog/Flyout), or respond to navigation | "-"<br>"--Profile"<br>"-/Login" | Navigate back one page (in a frame)<br>Navigate to `ProfilePage` and remove two pages from back stack<br>Navigate to `LoginPage` and clear back stack |

> [!NOTE]
> Besides using qualifiers as a string as part of the route specification, a `Qualifiers` class is also provided under the `Uno.Extensions.Navigation` namespace and can be specified when navigating, for example, `await navigator.NavigateViewModelAsync<MainViewModel>(this, Qualifiers.ClearBackStack);`.
