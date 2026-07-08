using Microsoft.UI.Xaml.Controls;

namespace Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;

/// <summary>
/// Shell page — acts as the root content control for the navigation host.
/// Replicates the drivernav Shell (UserControl + IContentControlProvider).
/// </summary>
public sealed partial class ShellPage : ContentControl
{
	public ShellPage()
	{
		HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
	}
}
