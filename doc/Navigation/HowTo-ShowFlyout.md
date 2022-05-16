# How-To: Show a Flyout

This topic walks through using Navigation to display a modal flyout

> [!Tip] This guide assumes you used the Uno.Extensions `dotnet new unoapp-extensions` template to create the solution. Instructions for creating an application from the template can be found [here](../Extensions/GettingStarted/UsingUnoExtensions.md)

## Step-by-steps


- Add new page, SamplePage.xaml
- Add a button "Go to Sample Page" and in the event handler  

**C#**  
```csharp
    private void ShowFlyoutClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier: Qualifiers.Dialog);
    }
```


<Button Content="Show flyout from XAML"
        HorizontalAlignment="Stretch"
        uen:Navigation.Request="!Sample" />

- Show how to navigate to a flyout
- Show how to navigate to a page with ! qualifier to allow navigation within flyout
