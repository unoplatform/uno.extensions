using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyRecordView : UserControl
{
	public ReadOnlyRecordView()
	{
		this.InitializeComponent();

		this.DataContext = new BindableReadOnlyRecordViewModel();
	}
}
