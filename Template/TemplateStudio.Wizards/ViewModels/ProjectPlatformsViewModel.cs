using System.Collections.Generic;
using TemplateStudio.Wizards.ComponentModel;

namespace TemplateStudio.Wizards.ViewModels;

internal class ProjectPlatformsViewModel : WizardViewModelBase
{
	public ProjectPlatformsViewModel(IDictionary<string, string> replacementsDictionary)
		: base(replacementsDictionary)
	{
	}

	[TemplateParameter("android")]
	public bool Android
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("ios")]
	public bool iOS
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("maccatalyst")]
	public bool MacCatalyst
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("skia-linux-fb")]
	public bool LinuxFrameBuffer
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("skia-gtk")]
	public bool Gtk
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("skia-wpf")]
	public bool Wpf
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("wasm")]
	public bool WebAssembly
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("winAppSdk")]
	public bool WinUI
	{
		get => Get<bool>();
		set => Set(value);
	}

	[TemplateParameter("server")]
	public bool Server
	{
		get => Get<bool>();
		set => Set(value);
	}
}
