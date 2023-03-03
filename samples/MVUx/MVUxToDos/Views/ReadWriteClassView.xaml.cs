using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadWriteClassView : UserControl
{
    public ReadWriteClassView()
    {
        this.InitializeComponent();

		this.DataContext = new BindableReadWriteClassViewModel();
    }
}
