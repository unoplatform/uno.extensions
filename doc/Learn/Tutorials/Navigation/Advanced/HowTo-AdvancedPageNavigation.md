---
uid: Learn.Tutorials.Navigation.Advanced.PageNavigation
---
# How-To: Advanced Page Navigation

Sometimes when you navigate you don't want to leave the current page in the back-stack. For example after signing into an application, you might want to navigate to the main page of the application; you don't want to have the login page still in the back-stack for a user to accidentally to go back to. 

> [!TIP]
> This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions-net6` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

> [!IMPORTANT]
> The `unoapp-extensions-net6` template requires the following changes for this tutorial:
1. Add the following inside the `MainPage` class in `MainPage.xaml.cs' 
    ```csharp
    public MainViewModel? ViewModel => DataContext as MainViewModel;
    ```
    
2. Replace `Content="Go to Second Page"` with `Click="{x:Bind ViewModel.GoToSecondPage}"` in `MainPage.xaml`

## Step-by-steps


### 1. Navigating to a Page and Clearing Back Stack

- Update the `GoToSecondPage` method in `MainViewModel` to include the `Qualifiers.ClearBackStack` argument.
    ```csharp
    public async Task GoToSecondPage()
    {
    	await _navigator.NavigateViewModelAsync<SecondViewModel>(this, qualifier: Qualifiers.ClearBackStack);
    }
    ```

If you run the application and navigate to the SecondPage the back button in the `NavigationBar` isn't visible, since the frame back-stack is empty.

### 2. Navigating to a Page and Removing a Page from Back Stack

Another common scenario is to navigate to a page and then remove the current page from the back stack.



- Clear stack



>> Go back and forward to different page eg ./NewPage
>> Go to multiple pages eg NextPage/AnotherPage





- 