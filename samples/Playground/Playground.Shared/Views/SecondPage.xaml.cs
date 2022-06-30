
namespace Playground.Views;


public sealed partial class SecondPage : Page
{
	public Country SelectedCountry { get; } = new Country("Australia");

	public SecondPage()
	{
		this.InitializeComponent();
	}
}
