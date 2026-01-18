---
name: uno-navigation-skills-index
description: Complete index of all Agent Skills for Uno Platform Navigation Extensions. Use as a starting point to find the right skill for your navigation implementation needs.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Uno Platform Navigation Extensions - Skills Index

This document provides a complete index of all Agent Skills for implementing navigation in Uno Platform applications using Navigation Extensions and Region-based Navigation.

## Skills Overview

| Skill | Description | Use When |
|-------|-------------|----------|
| [uno-navigation-setup](uno-navigation-setup/SKILL.md) | Initial setup and configuration | Starting a new project with navigation |
| [uno-navigation-routes](uno-navigation-routes/SKILL.md) | Route registration with ViewMap/RouteMap | Defining navigation routes |
| [uno-navigation-regions](uno-navigation-regions/SKILL.md) | Region-based navigation concepts | Understanding region hierarchy |
| [uno-navigation-code](uno-navigation-code/SKILL.md) | Programmatic navigation with INavigator | Navigating from ViewModels/code |
| [uno-navigation-xaml](uno-navigation-xaml/SKILL.md) | Declarative XAML navigation | Navigation from buttons/controls |
| [uno-navigation-data](uno-navigation-data/SKILL.md) | Data passing between pages | Sending data to target pages |
| [uno-navigation-dialogs](uno-navigation-dialogs/SKILL.md) | Dialogs, flyouts, and modals | Showing overlay content |
| [uno-navigation-qualifiers](uno-navigation-qualifiers/SKILL.md) | Back stack management | Controlling navigation history |
| [uno-navigation-tabbar](uno-navigation-tabbar/SKILL.md) | TabBar navigation | Bottom tab navigation |
| [uno-navigation-navigationview](uno-navigation-navigationview/SKILL.md) | NavigationView navigation | Sidebar/hamburger navigation |
| [uno-navigation-contentcontrol](uno-navigation-contentcontrol/SKILL.md) | ContentControl regions | Dynamic content areas |
| [uno-navigation-panel-visibility](uno-navigation-panel-visibility/SKILL.md) | Visibility-based navigation | Lightweight content switching |
| [uno-navigation-responsive-shell](uno-navigation-responsive-shell/SKILL.md) | Responsive navigation shells | Adaptive mobile/desktop navigation |
| [uno-navigation-message-dialog](uno-navigation-message-dialog/SKILL.md) | Message dialog display | Alerts and confirmations |
| [uno-navigation-troubleshooting](uno-navigation-troubleshooting/SKILL.md) | Common issues and solutions | Debugging navigation problems |

---

## Skills by Category

### Getting Started

1. **[uno-navigation-setup](uno-navigation-setup/SKILL.md)**
   - UnoFeatures configuration
   - App.xaml.cs setup
   - Shell page creation
   - INavigator dependency injection

2. **[uno-navigation-routes](uno-navigation-routes/SKILL.md)**
   - ViewMap types (ViewMap, DataViewMap, ResultDataViewMap)
   - RouteMap configuration
   - Nested routes and dependencies
   - Default routes and redirects

3. **[uno-navigation-regions](uno-navigation-regions/SKILL.md)**
   - Region.Attached property
   - Region.Name for targeting
   - Region.Navigator for control types
   - Region hierarchy patterns

### Navigation Methods

4. **[uno-navigation-code](uno-navigation-code/SKILL.md)**
   - INavigator interface usage
   - NavigateRouteAsync, NavigateViewModelAsync, NavigateViewAsync
   - Result-based navigation
   - Multi-page navigation patterns

5. **[uno-navigation-xaml](uno-navigation-xaml/SKILL.md)**
   - Navigation.Request attached property
   - Qualifier prefixes (-, -/, !, ./)
   - Navigation.Data binding
   - List navigation patterns

### Data Management

6. **[uno-navigation-data](uno-navigation-data/SKILL.md)**
   - DataViewMap for typed data
   - Constructor injection
   - Polymorphic routing
   - Round-trip data patterns

7. **[uno-navigation-qualifiers](uno-navigation-qualifiers/SKILL.md)**
   - Qualifiers.None
   - Qualifiers.NavigateBack (-)
   - Qualifiers.ClearBackStack (-/)
   - Qualifiers.Dialog (!)

### Navigation Controls

8. **[uno-navigation-tabbar](uno-navigation-tabbar/SKILL.md)**
   - Uno Toolkit TabBar setup
   - BottomTabBarStyle and VerticalTabBarStyle
   - Region linking with TabBarItems
   - Data passing and responsive layouts

9. **[uno-navigation-navigationview](uno-navigation-navigationview/SKILL.md)**
   - WinUI NavigationView integration
   - Menu items with Region.Name
   - Settings item navigation
   - Hierarchical navigation

10. **[uno-navigation-contentcontrol](uno-navigation-contentcontrol/SKILL.md)**
    - ContentControl as navigation host
    - Named regions for multiple areas
    - Split-view patterns
    - Conditional content display

11. **[uno-navigation-panel-visibility](uno-navigation-panel-visibility/SKILL.md)**
    - Region.Navigator="Visibility"
    - Panel/Grid content switching
    - Pre-rendered content patterns
    - Toggle and tab patterns

### Dialogs and Overlays

12. **[uno-navigation-dialogs](uno-navigation-dialogs/SKILL.md)**
    - Page-as-dialog patterns
    - ContentDialog navigation
    - Flyout and popup navigation
    - Result handling from dialogs

13. **[uno-navigation-message-dialog](uno-navigation-message-dialog/SKILL.md)**
    - ShowMessageDialogAsync usage
    - DialogAction configuration
    - Confirmation patterns
    - Error and alert dialogs

### Advanced Patterns

14. **[uno-navigation-responsive-shell](uno-navigation-responsive-shell/SKILL.md)**
    - Responsive markup extension
    - TabBar ↔ NavigationView switching
    - Shared content areas
    - Custom breakpoints

### Debugging

15. **[uno-navigation-troubleshooting](uno-navigation-troubleshooting/SKILL.md)**
    - Common setup issues
    - Route registration problems
    - Region hierarchy debugging
    - Performance optimization

---

## Quick Start Guide

### New Project Setup

1. Start with **[uno-navigation-setup](uno-navigation-setup/SKILL.md)**
2. Define routes using **[uno-navigation-routes](uno-navigation-routes/SKILL.md)**
3. Set up regions with **[uno-navigation-regions](uno-navigation-regions/SKILL.md)**

### Choose Navigation Pattern

- **Mobile app with tabs** → [uno-navigation-tabbar](uno-navigation-tabbar/SKILL.md)
- **Desktop app with sidebar** → [uno-navigation-navigationview](uno-navigation-navigationview/SKILL.md)
- **Responsive app** → [uno-navigation-responsive-shell](uno-navigation-responsive-shell/SKILL.md)
- **Simple content switching** → [uno-navigation-panel-visibility](uno-navigation-panel-visibility/SKILL.md)

### Implement Navigation

- **From XAML** → [uno-navigation-xaml](uno-navigation-xaml/SKILL.md)
- **From code/ViewModel** → [uno-navigation-code](uno-navigation-code/SKILL.md)
- **Passing data** → [uno-navigation-data](uno-navigation-data/SKILL.md)

### Add Dialogs

- **Message dialogs** → [uno-navigation-message-dialog](uno-navigation-message-dialog/SKILL.md)
- **Custom dialogs** → [uno-navigation-dialogs](uno-navigation-dialogs/SKILL.md)

### Debug Issues

- **Something not working** → [uno-navigation-troubleshooting](uno-navigation-troubleshooting/SKILL.md)

---

## Key Concepts Reference

### XAML Namespaces

```xml
xmlns:uen="using:Uno.Extensions.Navigation.UI"
xmlns:utu="using:Uno.Toolkit.UI"
xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
```

### Required UnoFeatures

```xml
<UnoFeatures>Navigation</UnoFeatures>
<!-- Add Toolkit for TabBar, NavigationBar -->
<UnoFeatures>Navigation;Toolkit</UnoFeatures>
```

### Core Attached Properties

| Property | Purpose |
|----------|---------|
| `Region.Attached` | Enables navigation on element |
| `Region.Name` | Identifies region for targeting |
| `Region.Navigator` | Specifies navigator type (e.g., "Visibility") |
| `Navigation.Request` | Declares navigation route |
| `Navigation.Data` | Binds data to pass |

### Navigation Qualifiers

| Qualifier | XAML Prefix | Effect |
|-----------|-------------|--------|
| `Qualifiers.None` | (none) | Normal forward navigation |
| `Qualifiers.NavigateBack` | `-` | Navigate back |
| `Qualifiers.ClearBackStack` | `-/` | Clear stack, then navigate |
| `Qualifiers.Dialog` | `!` | Open as dialog |
| Relative | `./` | Navigate within current region |

### INavigator Extension Methods

| Method | Purpose |
|--------|---------|
| `NavigateRouteAsync` | Navigate by route name |
| `NavigateViewModelAsync<T>` | Navigate by ViewModel type |
| `NavigateViewAsync<T>` | Navigate by View type |
| `NavigateDataAsync` | Navigate with data |
| `NavigateBackAsync` | Navigate back |
| `NavigateBackWithResultAsync` | Navigate back with result |
| `GetDataAsync<T>` | Get navigation data |
| `ShowMessageDialogAsync` | Display message dialog |

---

## Version Information

- **Skills Version**: 1.0
- **Target**: Uno Platform 5.x with Uno.Extensions
- **Framework**: WinUI 3 / WinAppSDK

---

## Additional Resources

- [Uno Platform Documentation](https://platform.uno/docs/)
- [Uno.Extensions.Navigation Documentation](https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Navigation/NavigationOverview.html)
- [Uno Toolkit Documentation](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/getting-started.html)
