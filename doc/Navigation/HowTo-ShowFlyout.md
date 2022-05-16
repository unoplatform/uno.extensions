# How-To: Show a Flyout

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
