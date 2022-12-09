using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly ILocalizationService _localizationService;
	public ValidationOneViewModel()
	{
		

	}

}

