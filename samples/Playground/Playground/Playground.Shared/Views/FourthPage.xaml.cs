namespace Playground.Views;

public sealed partial class FourthPage : Page, IInjectable<INavigator>
{
	public FourthViewModel? ViewModel { get; private set; }

	private INavigator? Navigator { get; set; }
	
	public FourthPage()
	{
		this.InitializeComponent();

		DataContextChanged += FourthPage_DataContextChanged;
	}

	private void FourthPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		ViewModel = args.NewValue as FourthViewModel;
	}

	private void FifthPageClick(object sender, RoutedEventArgs args)
	{
		Navigator?.NavigateViewAsync<FifthPage>(this);
	}

	public void Inject(INavigator entity)
	{
		Navigator = entity;
	}
}
