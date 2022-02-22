using Playground.Models;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Playground.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SecondPage : Page
    {
	public Country SelectedCountry { get; } = new Country("Australia");

	public SecondPage()
        {
            this.InitializeComponent();
        }
    }
