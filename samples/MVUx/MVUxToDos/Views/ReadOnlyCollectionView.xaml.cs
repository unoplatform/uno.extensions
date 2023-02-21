using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyCollectionView : UserControl
{
    public ReadOnlyCollectionView()
    {
        this.InitializeComponent();

        this.DataContext = new BindableReadOnlyCollectionViewModel();
    }
}
