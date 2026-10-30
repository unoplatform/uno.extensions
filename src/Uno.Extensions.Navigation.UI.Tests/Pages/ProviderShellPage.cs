using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// A Page shell that is its own <see cref="IContentControlProvider"/>. Routing to it nests a second instance inside it.
/// </summary>
public sealed partial class ProviderShellPage : Page, IContentControlProvider
{
	private readonly ContentControl _content = new()
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch,
	};

	public ProviderShellPage()
	{
		Content = _content;
	}

	public ContentControl ContentControl => _content;
}
