using CommunityToolkit.Mvvm.ComponentModel;
using Playground.Models;

namespace Playground.ViewModels;

public partial class XamlViewModel:ObservableObject
{
	[ObservableProperty]
	private Country country;

	public XamlViewModel()
	{
		Country = new Country("France");
	}
}
