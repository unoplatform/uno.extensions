using Microsoft.UI.Xaml.Controls;
using MVUxToDos.Presentation;

namespace MVUxToDos.Views;

public sealed partial class ReadOnlyClassLoadRefreshView : UserControl
{
	public ReadOnlyClassLoadRefreshView()
	{
		this.InitializeComponent();

		this.DataContext = new BindableReadOnlyClassLoadRefreshViewModel();
	}
}
