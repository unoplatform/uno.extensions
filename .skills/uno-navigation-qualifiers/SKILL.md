---
name: uno-navigation-qualifiers
description: Use navigation qualifiers in Uno Platform to control back stack behavior. Use when clearing navigation history, removing pages from back stack, opening dialogs, or navigating to nested regions. Covers all qualifier prefixes and the Qualifiers class.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-navigation
---

# Navigation Qualifiers

This skill covers using navigation qualifiers to control back stack behavior and navigation modes.

## Qualifiers Overview

Qualifiers modify how navigation behaves, particularly regarding the back stack.

## XAML Qualifier Prefixes

| Prefix | Description | Example |
|--------|-------------|---------|
| (none) | Normal forward navigation | `Products` |
| `-` | Navigate back | `-` |
| `-/` | Clear back stack, then navigate | `-/Login` |
| `-Route` | Remove current page, navigate forward | `-NextPage` |
| `!` | Open as dialog (flyout/modal) | `!Options` |
| `./` | Navigate to nested region | `./Details` |
| `../` | Navigate to parent region | `../Main` |

## Code Qualifiers Class

The `Qualifiers` class provides constants for use in code:

```csharp
using Uno.Extensions.Navigation;

// Available qualifiers
Qualifiers.None           // Normal navigation
Qualifiers.NavigateBack   // Go back (-)
Qualifiers.ClearBackStack // Clear back stack (-/)
Qualifiers.Dialog         // Open as dialog (!)
```

## Usage Examples

### Navigate Back

**XAML:**
```xml
<Button Content="Go Back" uen:Navigation.Request="-" />
```

**Code:**
```csharp
await _navigator.NavigateBackAsync(this);
```

### Clear Back Stack

Use when navigating to a point of no return (like after login):

**XAML:**
```xml
<Button Content="Skip to Home" uen:Navigation.Request="-/Main" />
```

**Code:**
```csharp
await _navigator.NavigateViewModelAsync<MainViewModel>(
    this, 
    qualifier: Qualifiers.ClearBackStack);
```

**Common Use Cases:**
- After successful login → navigate to main with cleared stack
- Completing onboarding → navigate to home with cleared stack
- Logout → navigate to login with cleared stack

### Remove Current Page

Navigate forward but remove the current page from the back stack:

**XAML:**
```xml
<Button Content="Continue" uen:Navigation.Request="-NextStep" />
```

**Code:**
```csharp
await _navigator.NavigateViewModelAsync<NextStepViewModel>(
    this, 
    qualifier: Qualifiers.NavigateBack);
```

**Use Cases:**
- Multi-step wizards where you don't want to go back to intermediate steps
- Replacing current content without keeping it in history

### Open as Dialog

**XAML:**
```xml
<Button Content="Show Options" uen:Navigation.Request="!Options" />
```

**Code:**
```csharp
await _navigator.NavigateViewAsync<OptionsPage>(
    this, 
    qualifier: Qualifiers.Dialog);
```

### Nested Region Navigation

Navigate within child regions:

**XAML:**
```xml
<!-- Navigate to 'Products' region within current page -->
<Button Content="Products" uen:Navigation.Request="./Products" />

<!-- Navigate to specific content in a named region -->
<Button Content="Product Details" uen:Navigation.Request="./Details/ProductInfo" />
```

### Parent Region Navigation

Navigate up to parent regions:

```xml
<Button Content="Back to Main" uen:Navigation.Request="../Main" />
```

## Multi-Page Navigation

Navigate through multiple pages in one action:

**XAML:**
```xml
<Button Content="Go to Sample" uen:Navigation.Request="Second/Sample" />
```

**Code:**
```csharp
await _navigator.NavigateRouteAsync(this, "Second/Sample");
```

This navigates to `Sample` with `Second` injected into the back stack.

## Combining Qualifiers

### Clear Stack and Navigate to Nested

```xml
<Button Content="Start Fresh" uen:Navigation.Request="-/Main/Home" />
```

### Dialog with Data

```xml
<Button Content="Edit" 
        uen:Navigation.Request="!EditDialog"
        uen:Navigation.Data="{Binding SelectedItem}" />
```

## Practical Scenarios

### Login Flow

```xml
<!-- On Login Success -->
<Button Content="Enter App" uen:Navigation.Request="-/Main" />
```

### Onboarding Skip

```xml
<Button Content="Skip" uen:Navigation.Request="-/Login" />
```

### Wizard Steps

```xml
<!-- Step 1 to Step 2, remove Step 1 from history -->
<Button Content="Next" uen:Navigation.Request="-Step2" />

<!-- Final step, clear all and go to completion -->
<Button Content="Finish" uen:Navigation.Request="-/Completion" />
```

### Settings with Sub-Pages

```xml
<!-- Main settings -->
<Button Content="Account Settings" uen:Navigation.Request="./Account" />
<Button Content="Privacy Settings" uen:Navigation.Request="./Privacy" />
```

### Confirmation Dialog

```xml
<Button Content="Delete" uen:Navigation.Request="!ConfirmDelete" />
```

## Code-Based Qualifier Usage

```csharp
// Normal navigation
await _navigator.NavigateRouteAsync(this, "Products");

// Clear back stack
await _navigator.NavigateRouteAsync(this, "Main", qualifier: Qualifiers.ClearBackStack);

// Dialog
await _navigator.NavigateRouteAsync(this, "Options", qualifier: Qualifiers.Dialog);

// Custom qualifier string
await _navigator.NavigateRouteAsync(this, "Details", qualifier: "./");
```

## Best Practices

1. **Use `-/` for authentication boundaries** - after login/logout

2. **Use `!` for temporary UI** - confirmations, pickers, filters

3. **Use `./` for in-page region navigation** - tabs, sidebars

4. **Use `-Route` sparingly** - only when you specifically want to remove the current page

5. **Consider user expectations** - users expect Back to go to the previous meaningful page

6. **Document navigation flows** - especially when using complex qualifier combinations

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Back button still shows previous page | Use `-/` instead of normal navigation |
| Dialog doesn't open | Ensure `!` prefix is used |
| Nested region doesn't update | Verify `./` prefix and `Region.Attached="True"` |
| Multi-page navigation fails | Check all routes in the path are registered |
