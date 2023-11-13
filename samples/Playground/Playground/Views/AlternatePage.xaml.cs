namespace Playground.Views;

public sealed partial class AlternatePage : Page
    {
        public AlternatePage()
        {
            this.InitializeComponent();
        }

	private void BackClick(object sender, RoutedEventArgs e)
	{
		this.Frame.GoBack();
	}

}
