//-:cnd:noEmit

namespace MyExtensionsApp.Views;

public sealed partial class SecondPage : Page
{
	public SecondViewModel? ViewModel => DataContext as SecondViewModel;
	public SecondPage()
    {
        this.InitializeComponent();
    }
}

