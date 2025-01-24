---
uid: Uno.Extensions.Navigation.Overview
---

# Introduction

## What is Navigation?

Navigation needs to encompass a range of UI concepts:

* Navigation between pages in a <a href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.frame" target="_blank">`Frame`</a> (forward and backwards)
* Switching between menu items in a <a href="https://learn.microsoft.com/windows/winui/api/microsoft.ui.xaml.controls.navigationview?view=winui-3.0" target="_blank">`NavigationView`</a>, or between tab items in a <a href="https://platform.uno/uno-toolkit/" target="_blank">`TabBar`</a>
* Loading content into a <a href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.contentcontrol" target="_blank">ContentControl</a>
* Loading and toggling visibility of child elements in a <a href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.grid" target="_blank">`Grid`</a>
* Displaying a <a href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.primitives.popup" target="_blank">`Popup`</a> or <a href="https://learn.microsoft.com/windows/apps/design/controls/dialogs-and-flyouts/flyouts" target="_blank">`Flyout`</a>
* Prompt a <a href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.controls.contentdialog" target="_blank">`ContentDialog`</a> or <a href="https://learn.microsoft.com/uwp/api/windows.ui.popups.messagedialog" target="_blank">`MessageDialog`</a>

[!include[getting-help](../includes/getting-help.md)]

## Installation

`Navigation` is provided as an Uno Feature. To enable `Navigation` support in your application, add `Navigation` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

## What triggers Navigation?

Navigation can be triggered for a number of reasons:

* Change view
Either based on the type of View or the type of ViewModel to show
* Display data
The view to show is based on the type of data to display (eg display ProductX)
* Prompt, or request, for data
The view to show is based on the type of data being requested (eg Country picker)
Prompt the user using a flyout or content dialog to get a response

## Architecture Objectives

### Navigation needs to be accessible from anywhere

* View (Code behind)
i.e. in context of a page/usercontrol
* View (XAML)
i.e. using attached properties
* ViewModel (Presentation)
i.e. in a context that doesn't have access to the UI layer

### Navigation needs to make use of available data

* Uri
Used to share links to the app (eg deeplink)
* DTO
If an instance of an entity is already in memory, navigation should support passing the existing entity between views
* ViewModel
The type of viewmodel (associated with the view) to navigate to
* View
The type of view to navigate to

## Common Scenarios

### 1. Navigating between pages (code behind)

* Navigate forward to a new page by calling `NavigateRouteAsync` with the route to navigate to

    **XAML**

    ```xml
    <Page x:Class="Playground.Views.HomePage">
        <Button Click="{x:Bind GoToSecondPageClick}"
                Content="Go to Second Page - Code behind" />
    </Page>
    ```

    **C#**

    ```csharp
    public sealed partial class HomePage : Page
    {
        public async void GoToSecondPageClick()
        {
            var nav = this.Navigator();
            await nav.NavigateRouteAsync(this, "Second");
        }
    }
    ```

* Navigate back to the previous page by calling `NavigateBackAsync`

    **XAML**

    ```xml
    <Page x:Class="Playground.Views.SecondPage">
        <Button Click="{x:Bind GoBackClick}"
                Content="Go Back" />
    </Page>
    ```

    **C#**

    ```csharp
    public sealed partial class SecondPage : Page
    {
        public async void GoBackClick()
        {
            var nav = this.Navigator();
            await nav.NavigateBackAsync(this);
        }
    }
    ```

### 2. Navigating between view models

* Navigate forward to a new page by calling `NavigateViewModelAsync` with the route to navigate to

    **XAML**

    ```xml
    <Page x:Class="Playground.Views.HomePage">
        <Button Click="{x:Bind ViewModel.GoToSecondPageClick}"
                Content="Go to Second Page - View Model" />
    </Page>
    ```

    **C#**

    ```csharp
    public sealed partial class HomeViewModel
    {
        private readonly INavigator _navigator;
        public HomeViewModel(INavigator navigator)
        {
            _navigator = navigator;
        }

        public async void GoToSecondPageClick()
        {
            await _navigator.NavigateViewModelAsync<SecondViewModel>(this);
        }
    }
    ```

* Navigate back to the previous page by calling `NavigateBackAsync`

    **XAML**

    ```xml
    <Page x:Class="Playground.Views.SecondPage">
        <Button Click="{x:Bind ViewModel.GoBackClick}"
                Content="Go Back" />
    </Page>
    ```

    **C#**

    ```csharp
    public sealed partial class SecondViewModel
    {
        private readonly INavigator _navigator;
        public SecondViewModel(INavigator navigator)
        {
            _navigator = navigator;
        }

        public async void GoBackClick()
        {
            await _navigator.NavigateBackAsync(this);
        }
    }
    ```

### 3. Navigating between pages (XAML)

* Navigate forward to new page by specifying the route in the `Navigation.Request` attached property

    **XAML**

    ```xml
    <Page x:Class="Playground.Views.HomePage">
        <Button uen:Navigation.Request="Second"
                Content="Go to Second Page - XAML" />
    </Page>
    ```

* Navigate to previous page by specifying the back qualifier in the `Navigation.Request` attached property
    **XAML**

    ```xml
    <Page x:Class="Playground.Views.SecondPage">
        <Button uen:Navigation.Request="-"
                Content="Go Back" />
    </Page>
    ```

### 4. Navigating between pages (XAML)

* Navigate forward to new page and clearing back-stack by specifying the route in the `Navigation.Request` attached property with the clear back-stack qualifier ("-/")
    **XAML**

    ```xml
    <Page x:Class="Playground.Views.HomePage">
        <Button uen:Navigation.Request="-/Second"
                Content="Go to Second Page - XAML" />
    </Page>
    ```

### 5. Prompt user - Message Dialog

* Prompt the user with a message using the `ShowMessageDialogAsync` method.
    **C#**

    ```csharp
    public sealed partial class HomePage : Page
    {
        public async void PromptWithMessageDialogClick()
        {
            var nav = this.Navigator();
            var messageResult = await nav.ShowMessageDialogAsync(this,"Warning about something","Alert");
        }
    }
    ```

### 6. Switching items in NavigationView

* Define selectable navigation view items by setting the `Region.Name`. When an item is selected, the child element of the `Grid` with the same `Region.Name` will be set to `Visible` (and the others set back to `Collapsed`)
    **XAML**

    ```xml
    <Page x:Class="Playground.Views.NavigationViewPage">

        <Grid>
            <muxc:NavigationView uen:Region.Attached="true">
                <muxc:NavigationView.MenuItems>
                    <muxc:NavigationViewItem Content="Products"
                                             uen:Region.Name="Products" />
                    <muxc:NavigationViewItem Content="Deals"
                                             uen:Region.Name="Deals" />
                    <muxc:NavigationViewItem Content="Profile"
                                             uen:Region.Name="Profile" />
                </muxc:NavigationView.MenuItems>
                <Grid uen:Region.Attached="True">
                    <StackPanel uen:Region.Name="Products"
                                Visibility="Collapsed">
                        <TextBlock Text="Products" />
                    </StackPanel>
                    <StackPanel uen:Region.Name="Deals"
                                Visibility="Collapsed">
                        <TextBlock Text="Deals" />
                    </StackPanel>
                    <StackPanel uen:Region.Name="Profile"
                                Visibility="Collapsed">
                        <TextBlock Text="Profile" />
                    </StackPanel>
                </Grid>
            </muxc:NavigationView>
        </Grid>
    </Page>
    ```

### 7. Displaying content in a ContentControl

* Specify the `Region.Name` of the nested control and the route of the `UserControl` to display in the `Navigation.Request` attached property
    **XAML**

    ```xml
    <Page x:Class="Playground.Views.ContentControlPage">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>
                <Button Content="Show profile"
                        uen:Navigation.Request="./Info/Profile" />
                <ContentControl uen:Region.Attached="True"
                                uen:Region.Name="Info"
                                Grid.Row="1" />
        </Grid>
    </Page>

    <UserControl x:Class="Playground.Views.ProfileUserControl">
    …
    </UserControl>
    ```
