namespace Playground.Views;

public sealed partial class CodeBehindPage : Page, IInjectable<INavigator>
{
	private INavigator? Navigator { get; set; }

	public void Inject(INavigator entity)
	{
		Navigator = entity;
	}

	public CodeBehindPage()
	{
		this.InitializeComponent();
	}


	// Navigate to a route eg Home
	public async void NavigateRouteAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateRouteAsync(this, "Second");
	}

	// Navigate to a route and expect a result of type TResult
	public async void NavigateRouteForResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.NavigateRouteForResultAsync<Widget>(this, "Second").AsResult();
	}

	// Navigate to a view of type TView
	public async void NavigateViewAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateViewAsync<SecondPage>(this);
	}

	// Navigate to a view of type TView and expect a result of type TResult
	public async void NavigateViewForResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.NavigateViewForResultAsync<SecondPage, Country>(this).AsResult();
	}

	// Navigate to a view model of type TViewModel
	public async void NavigateViewModelAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateViewModelAsync<SecondViewModel>(this);
	}

	// Navigate to a view model of type TViewModel and expect a result of type TResult
	public async void NavigateViewModelForResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.NavigateViewModelForResultAsync<SecondViewModel, Country>(this).AsResult();
	}

	// Navigate to the route that handles data of type TData
	public async void NavigateDataAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateDataAsync(this, new Widget("Test", 100.0));
	}

	// Navigate to the route that handles data of type TData and expect a result of type TResult
	public async void NavigateDataForResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.NavigateDataForResultAsync<Widget, Country>(this, new Widget("Test", 99.0)).AsResult();
	}

	// Navigate to the route that will return data of type TResultData
	public async void NavigateForResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.NavigateForResultAsync<Country>(this).AsResult();
	}

	// Navigate to the route that will return data of type TResultData
	public async void GetDataAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.GetDataAsync<Country>(this);
	}

	// Navigate to previous view (goback on frame or close dialog/popup)
	public async void NavigateBackAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateBackAsync(this);

	}

	// Navigate to previous view (goback on frame or close dialog/popup) and provide response data
	public async void NavigateBackWithResultAsyncClick(object sender, RoutedEventArgs args)
	{
		var response = await Navigator!.NavigateBackWithResultAsync(this, data: new Widget("Result", 80.0));

	}

	// Show MessageDialog
	public async void ShowMessageDialogAsyncClick(object sender, RoutedEventArgs args)
	{
		var result = await Navigator!.ShowMessageDialogAsync<string>(this, content: "Sample content", title: "Sample title");
	}
}

