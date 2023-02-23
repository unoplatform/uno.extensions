using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyPrimitiveWAttributeLoadRefreshView : UserControl
{
    public ReadOnlyPrimitiveWAttributeLoadRefreshView()
    {
        this.InitializeComponent();

        this.DataContext = new BindableReadOnlyPrimitiveWAttributeLoadRefreshViewModel();
    }
}
