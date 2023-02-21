using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyScalarLoadRefreshView : UserControl
{
    public ReadOnlyScalarLoadRefreshView()
    {
        this.InitializeComponent();

        this.DataContext = new BindableReadOnlyScalarLoadRefreshViewModel();
    }
}