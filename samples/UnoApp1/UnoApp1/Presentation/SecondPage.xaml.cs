namespace UnoApp1.Presentation;

public sealed partial class SecondPage : Page
{
    public SecondPage()
    {
        this.InitializeComponent();

        DataContextChanged += SecondPage_DataContextChanged;
    }

    private void SecondPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
    }
}

