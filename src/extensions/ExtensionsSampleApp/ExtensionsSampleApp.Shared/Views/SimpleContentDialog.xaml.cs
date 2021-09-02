using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ExtensionsSampleApp.Views
{
    public sealed partial class SimpleContentDialog : ContentDialog
    {
        public SimpleContentDialog()
        {
            this.InitializeComponent();
        }

        private void TermsOfUseContentDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            // Ensure that the check box is unchecked each time the dialog opens.
            ConfirmAgeCheckBox.IsChecked = false;
        }

        private void ConfirmAgeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            this.IsPrimaryButtonEnabled = true;
        }

        private void ConfirmAgeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            this.IsPrimaryButtonEnabled = false;
        }
    }
}
