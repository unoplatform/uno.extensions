using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;

/// <summary>
/// Replicates drivernav's SiblingPage — a ResultDataViewMap target
/// that can both receive and return data of type <see cref="ResultEntity"/>.
/// It also supports navigating forward to SiblingTwoPage via GetDataAsync (chained).
/// </summary>
public sealed partial class SiblingPage : Page
{
	public SiblingPage()
	{
		Content = new TextBlock { Text = "Sibling Page" };
	}
}
