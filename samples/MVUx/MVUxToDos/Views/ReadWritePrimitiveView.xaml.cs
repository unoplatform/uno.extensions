using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadWritePrimitiveView : UserControl
{
    public ReadWritePrimitiveView()
    {
        this.InitializeComponent();

		this.DataContext = new BindableReadWritePrimitiveViewModel();
    }
}
