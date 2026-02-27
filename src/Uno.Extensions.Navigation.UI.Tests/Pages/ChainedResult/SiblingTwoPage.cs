using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;

/// <summary>
/// Replicates drivernav's SiblingTwoPage — a ResultDataViewMap target
/// that can receive and return data of type <see cref="ResultEntity"/>.
/// It also supports navigating forward to SecondPage via GetDataAsync (three-level chain).
/// </summary>
public sealed partial class SiblingTwoPage : Page
{
	public SiblingTwoPage()
	{
		Content = new TextBlock { Text = "Sibling Two Page" };
	}
}
