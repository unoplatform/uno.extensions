---
uid: Learn.Tutorials.Navigation.Advanced.PageNavigation
---
# How-To: Employ Advanced Page Navigation Techniques

When using navigation, you may not want to allow the current page to remain in the back-stack. If you want to navigate to the main page of the application after signing in, there should not be a login page still in the back-stack for a user to accidentally to go back to.

Using backward/forward navigation in your app requires a degree of extra consideration. Users always expect the back/forward button to take them to a page that is still relevant, yet logically related to the current page and direction. This page contains several concise tutorials about how to implement navigation techniques that address these more advanced problems.

## Techniques

### Navigating to a Page and Clearing Back Stack

- Add an additional button to `MainPage.xaml` with the `Click` event bound to the `GoToSecondPageClearBackStack` method

    ```csharp
    <StackPanel Grid.Row="1"
                HorizontalAlignment="Center"
                VerticalAlignment="Center">
        <Button AutomationProperties.AutomationId="SecondPageButton"
                Content="Go to Second Page"
                Click="{x:Bind ViewModel.GoToSecondPage}" />
        <Button Content="Go to Second Page Clear Stack"
                Click="{x:Bind ViewModel.GoToSecondPageClearBackStack}" />
    </StackPanel>
    ```

- Add the `GoToSecondPageClearBackStack` method in `MainViewModel` which navigates to the `SecondViewModel` and includes the `Qualifiers.ClearBackStack` qualifier.

    ```csharp
    public async Task GoToSecondPage()
    {
     await _navigator.NavigateViewModelAsync<SecondViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
    ```

If you run the application and navigate to the `SecondPage` the back button in the `NavigationBar` isn't visible, since the frame back-stack is empty.

### Navigating to a Page and Removing a Page from Back Stack

Another common scenario is to navigate to a page and then remove the current page from the back stack.

- Add a new `Page` to navigate to, `SamplePage.xaml`, in the UI (shared) project
- In `SecondPage.xaml` add a `Button` with the following XAML, which includes a handler for the Click event  

    ```xml
    <Button HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Content="Go to Sample Page"
            Click="{x:Bind ViewModel.GoToSamplePage}" />
    ```

- In `SecondPage.xaml.cs` add the following to expose a `ViewModel` property

    ```csharp
    public SecondViewModel? ViewModel => DataContext as SecondViewModel;
    ```

- Update `SecondViewModel` to include the following `GoToSamplePage` method

    ```csharp
    public async Task GoToSamplePage()
    {
        await _navigator.NavigateViewModelAsync<SampleViewModel>(this, qualifier: Qualifiers.NavigateBack);
    }
    ```

The use of `Qualifiers.NavigateBack` will result in the `SecondPage` being removed from the back stack, after navigating forward to the `SamplePage`.

### Navigating to Multiple Pages

In some cases you may want to navigate forward to a page and inject an additional page into the back stack. This can be done by specifying a multi-section route.

- In `MainPage.xaml` add the following XAML

    ```xml
    <Button Content="Go to Sample Page"
            Click="{x:Bind ViewModel.GoToSamplePage}" />
    ```

- Update `MainViewModel` to define the `GoToSamplePage` method

    ```csharp
    public async Task GoToSamplePage()
    {
        await _navigator.NavigateRouteAsync(this, route: "Second/Sample");
    }
    ```

This code uses the `NavigateRouteAsync` method that allows for string route to be specified. In this case the route is a multi-section route that will navigate to the `SamplePage` and inject the `SecondPage` into the back stack. Note that the `SecondPage` isn't actually created until the user navigates back from the `SamplePage`
