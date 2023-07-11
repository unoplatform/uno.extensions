---
uid: Reference.Markup.HowToCustomMarkupProjectToolkit
---

# Getting Started with Uno Toolkit

In the previous session we learn how to [Create your own C# Markup](xref:Reference.Markup.HowToCreateMarkupProject) and how [Custom your own C# Markup - Learn how to change Style, Bindings, Templates and Template Selectors using C# Markup](xref:Reference.Markup.HowToCustomMarkupProject).

Now we will check [Uno Toolkit](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/getting-started.html) with is a library that can be added to any new or existing Uno solution.

For this sample we will cover this controls:

- [NavigationBar](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/NavigationBar.html)  - Represents a specialized app bar that provides layout for AppBarButton and navigation logic.

- [Chip](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.html) - Chips are compact elements that represent an input, attribute, or action.

> If you haven't already set up your environment and create the new Markup project, please follow the steps to [Create your own C# Markup](xref:Reference.Markup.HowToCreateMarkupProject).

## NavigationBar

The NavigationBar is a user interface component used to provide navigation between different pages or sections of an application.

The navigation bar can include items such as a back button, a page title, and other navigation elements, depending on your application's structure and requirements.

### Changing UI to have the NavigationBar

- In the Shared Project open the file *MainPage.cs* and change the content to have the NavigationBar.

    # [**C# Markup**](#tab/cs)

    #### C# Markup

    ```csharp
    
    ```

    # [**XAML**](#tab/cli)
    
    #### XAML

    ```xml
    
    ```

    # [**Full Code**](#tab/code)

    #### Full C# Markup code
    
    - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
   
    ```

## Chip

A Chip is a user interface component used to display a specific selection or action in a compact format.

Chips are often used to display short pieces of information such as tags, categories, filters, or selected options. They provide a compact and interactive visual representation of this information, allowing users to efficiently view and interact with it.

### Changing UI to have the Chip

- In the Shared Project open the file *MainPage.cs* and change the content to have the Chip.

    # [**C# Markup**](#tab/cs)

    #### C# Markup

    ```csharp
    
    ```

    # [**XAML**](#tab/cli)
    
    #### XAML

    ```xml
    
    ```

    # [**Full Code**](#tab/code)

    #### Full C# Markup code
    
    - Example of the complete code on the MainPage.cs, so you can follow along in your own project.

    ```csharp
   
    ```


## Try it yourself

Now try to change your MainPage to have different layout and test other attributes and elements.

In this Tutorial we add the NavigationBar and some Chip to the UI.
But the Uno Toolkit has many other Controls as:

- [AutoLayout](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/AutoLayoutControl.html)
- [Cards](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/CardAndCardContentControl.html)
- [Chip and ChipGroup](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/ChipAndChipGroup.html)
- [DrawerControl](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/DrawerControl.html)
- [DrawerFlyoutPresenter](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/DrawerFlyoutPresenter.html)
- [LoadingView](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/LoadingView.html)
- [NavigationBar](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/NavigationBar.html)
- [SafeArea](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/SafeArea.html)
- [TabBar and TabBarItem](https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/controls/TabBarAndTabBarItem.html)

> Try to add another control as a learning exercise.

## Next Steps

- [Custom your own C# Markup - Learn how to change Visual States and User Controls](xref:Reference.Markup.HowToCustomMarkupProjectVisualStates)
- [Custom your own C# Markup - Learn how to use Toolkit](xref:Reference.Markup.HowToCustomMarkupProjectToolkit)
- [Custom your own C# Markup - Learn how to Change the Theme](xref:Reference.Markup.HowToCustomMarkupProjectTheme)
- [Custom your own C# Markup - Learn how to use MVUX](xref:Reference.Markup.HowToCustomMarkupProjectMVUX)
