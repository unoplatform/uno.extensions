namespace ApplicationTemplate
{
    public sealed partial class MainPage 
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.DataContextChanged += MainPage_DataContextChanged;
           
        }

        private void MainPage_DataContextChanged(Microsoft.UI.Xaml.FrameworkElement sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args)
        {
        }
    }
}
