using System.Collections.Generic;
using System.Linq;
using TemplateStudio.Wizards.ComponentModel;
using TemplateStudio.Wizards.Model;

namespace TemplateStudio.Wizards.ViewModels;

internal class AppConfigurationViewModel : WizardViewModelBase
{
	public AppConfigurationViewModel(IDictionary<string, string> replacementsDictionary)
		: base(replacementsDictionary)
	{
		TargetFramework = TargetFrameworkChoices.First();
		Presentation = PresentationChoices.First();
		Markup = MarkupChoices.First();
		AppTheme = AppThemeChoices.First();
	}

	[TemplateParameter("appid")]
	public string ApplicationId
	{
		get => Get<string>();
		set => Set(value);
	}

	[TemplateParameter("tfm")]
	public TemplateChoice TargetFramework
	{
		get => Get<TemplateChoice>();
		set => Set(value);
	}

	[TemplateParameter("architecture")]
	public TemplateChoice Presentation
	{
		get => Get<TemplateChoice>();
		set => Set(value);
	}

	[TemplateParameter("markup")]
	public TemplateChoice Markup
	{
		get => Get<TemplateChoice>();
		set => Set(value);
	}

	[TemplateParameter("theme")]
	public TemplateChoice AppTheme
	{
		get => Get<TemplateChoice>();
		set => Set(value);
	}

	public readonly TemplateChoice[] TargetFrameworkChoices = new TemplateChoice[]
	{
		new TemplateChoice(
			".NET 6.0 (Long Term Support)",
			"Uses the Long Term Support version of .NET",
			  "net6.0"),
		new TemplateChoice(
			".NET 7.0 (Standard Term Support)",
			"Uses the latest Standard Term support version of .NET",
			 "net7.0")
	};

	public readonly TemplateChoice[] PresentationChoices = new TemplateChoice[]
	{
		new TemplateChoice(
			"Mvvm (Community Toolkit)",
			"Uses the Community Toolkit to implement the MVVM design pattern.",
			"mvvm"),
		new TemplateChoice(
			"MVU-X (Reactive)",
			"Uses Uno.Extensions.Reactive to implement the MVU pattern with XAML Binding support",
			"mvux")
	};

	public readonly TemplateChoice[] MarkupChoices = new TemplateChoice[]
	{
		new TemplateChoice(
			"XAML",
			"Uses XAML files for UI Markup",
			"xaml"),
		new TemplateChoice(
			"C# Markup",
			"Uses the Uno C# Markup Extensions to build your UI in C# code.",
			"csharp")
	};

	public readonly TemplateChoice[] AppThemeChoices = new TemplateChoice[]
	{
		new TemplateChoice(
			"Material",
			"Uses the Material app theme",
			"material"),
		new TemplateChoice(
			"Fluent",
			"Uses the Fluent app theme",
			"fluent"),
		new TemplateChoice(
			"Cupertino",
			"Uses the Cupertino app theme",
			"cupertino")
	};
}
