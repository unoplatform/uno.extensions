---
uid: Learn.Tutorials.Navigation.Advanced.ContentControl
---
# How-To: Use a ContentControl to Display a View


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


### 1. 
# Navigating using a ContentControl

- Show how to use a ContentControl to display content