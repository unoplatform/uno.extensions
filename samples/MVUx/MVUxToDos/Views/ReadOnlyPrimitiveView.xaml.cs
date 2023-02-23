using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyPrimitiveView : UserControl
{
    public ReadOnlyPrimitiveView()
    {
        this.InitializeComponent();

        DataContext = new BindableReadOnlyPrimitiveViewModel();
    }
}
