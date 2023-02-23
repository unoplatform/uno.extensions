using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadWriteCollectionView : UserControl
{
	public ReadWriteCollectionView()
	{
		this.InitializeComponent();

		this.DataContext = new BindableReadWriteCollectionViewModel();
	}
}
