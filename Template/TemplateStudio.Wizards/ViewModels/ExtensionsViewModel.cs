using System.Collections.Generic;
using TemplateStudio.Wizards.ComponentModel;

namespace TemplateStudio.Wizards.ViewModels;

internal class ExtensionsViewModel : WizardViewModelBase
{
	public ExtensionsViewModel(IDictionary<string, string> replacementsDictionary)
		: base(replacementsDictionary)
	{
		Configuration = true;
		Logging = true;
		Serilog = true;
		Localization = true;
	}

	[TemplateParameter("Configuration")]
	public bool Configuration
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("Logging")]
	public bool Logging
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("Serilog")]
	public bool Serilog
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("Localization")]
	public bool Localization
	{
		get => Get<bool>();
		set => Set(value);
	}

}
