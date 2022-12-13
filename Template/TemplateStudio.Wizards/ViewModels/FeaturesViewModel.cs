using System.Collections.Generic;
using TemplateStudio.Wizards.ComponentModel;

namespace TemplateStudio.Wizards.ViewModels;

internal class FeaturesViewModel : WizardViewModelBase
{
	public FeaturesViewModel(IDictionary<string, string> replacementsDictionary)
		: base(replacementsDictionary)
	{
		Wpa = true;
		UnitTest = true;
		UITest = true;
	}

	[TemplateParameter("Wpa")]
	public bool Wpa
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("UnitTest")]
	public bool UnitTest
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("UITest")]
	public bool UITest
	{
		get => Get<bool>();
		set => Set(value);
	}


}
