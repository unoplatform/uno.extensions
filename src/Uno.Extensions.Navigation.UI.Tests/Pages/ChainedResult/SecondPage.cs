using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;

/// <summary>
/// Replicates drivernav's SecondPage — a ResultDataViewMap target
/// that can return data of type <see cref="ResultEntity"/> (terminal page).
/// </summary>
public sealed partial class SecondPage : Page
{
	public SecondPage()
	{
		Content = new TextBlock { Text = "Second Page" };
	}
}
