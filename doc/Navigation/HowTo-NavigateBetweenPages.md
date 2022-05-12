# How-To: Navigate Between Pages

- Add new page, SamplePage.xaml
- Add a button "Go to Sample Page" and in the event handler  

**C#**  
```csharp
    private void GoToSamplePageClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewAsync<SamplePage>(this);
    }
```

- On SamplePage add a button "Go back" and in event handler

**C#**  
```csharp
    private void GoBackClick(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateBackAsync(this);
    }
```

- On MainPage add another button and in event handler

**C#**  
```csharp
	private void GoToSamplePageClearStackClick(object sender, RoutedEventArgs e)
    {
		_ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier:Qualifiers.ClearBackStack);
    }
```
- After navigating to sample page, go back button doesn't work, since the frame backstack is empty
