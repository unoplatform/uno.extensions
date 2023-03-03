using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadWriteCollectionMasterDetailView : UserControl
{
	public ReadWriteCollectionMasterDetailView()
	{
		this.InitializeComponent();

		this.DataContext = new BindableReadWriteCollectionMasterDetailViewModel();
	}
}
