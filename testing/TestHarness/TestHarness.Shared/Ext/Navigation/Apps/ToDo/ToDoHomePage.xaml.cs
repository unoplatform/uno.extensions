namespace TestHarness.Ext.Navigation.Apps.ToDo;

public sealed partial class ToDoHomePage : Page
{
	public ToDoHomeViewModel? ViewModel => DataContext as ToDoHomeViewModel;
	public ToDoHomePage()
	{
		this.InitializeComponent();

		this.ApplyAdaptiveTrigger(App.Current.Resources["WideMinWindowWidth"] is double width ? width : 0.0, nameof(Narrow), nameof(Wide));

	}

	public async void NavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
	{
		await Task.Delay(500);
		if(App.Current.Resources["WideMinWindowWidth"] is double threshold &&
			this.ActualWidth< threshold)
		{
			sender.SelectedItem = null;
		}
	}

	public void SelectItem1Click(object sender, RoutedEventArgs e)
	{
		NavView.SelectedItem = (NavView.MenuItemsSource as IEnumerable<ToDoTaskList>)?.FirstOrDefault();
	}
	public void SelectItem2Click(object sender, RoutedEventArgs e)
	{
		NavView.SelectedItem = (NavView.MenuItemsSource as IEnumerable<ToDoTaskList>)?.Skip(1).FirstOrDefault();
	}
	public void SelectItem3Click(object sender, RoutedEventArgs e)
	{
		NavView.SelectedItem = (NavView.MenuItemsSource as IEnumerable<ToDoTaskList>)?.Skip(2).FirstOrDefault();
	}
}
