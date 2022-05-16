Sometimes when you navigate you don't want to leave the current page in the back-stack. For example after signing into an application, you might want to navigate to the main page of the application; you don't want to have the login page still in the back-stack for a user to accidentally to go back to (unless they explicitly log out of the application). 

- On MainPage add another button and in event handler

```xml
<Button Content="Go to SamplePage and clear stack"
        Click="GoToSamplePageClearStackClick" />
```

- This time, in the `GoToSamplePageClearStackClick` method specify `Qualifiers.ClearBackStack` for the qualifier argument to the `NavigateViewAsync` method.

```csharp
private void GoToSamplePageClearStackClick(object sender, RoutedEventArgs e)
{
    _ = this.Navigator()?.NavigateViewAsync<SamplePage>(this, qualifier:Qualifiers.ClearBackStack);
}
```

If you run the application and click on the Go to SamplePage and clear stack button, the go back button is disabled, since the frame back-stack is empty.


- Clear stack



>> Go back and forward to different page eg ./NewPage
>> Go to multiple pages eg NextPage/AnotherPage





- 