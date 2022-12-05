using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Locations;
using Microsoft.Templates.UI.Converters;
using Microsoft.Templates.UI.Styles;
using Microsoft.Templates.UI.VisualStudio.GenShell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;




using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.Templates.Core;
using Microsoft.Templates.Core.Diagnostics;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core.Helpers;
using Microsoft.Templates.Core.Locations;
using Microsoft.Templates.Core.PostActions.Catalog.Merge;
using Microsoft.Templates.SharedResources;
using Microsoft.Templates.UI.Launcher;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.Threading;
using Microsoft.Templates.UI.VisualStudio.GenShell;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Threading;


namespace TemplateStudio.Wizards
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IContextProvider
	{

		private string _platform;
		private string _appModel;
		private string _language;

		private readonly GenerationService _generationService = GenerationService.Instance;
		private UserSelection _userSelection;
		private Dictionary<string, string> _replacementsDictionary;
		public string SafeProjectName => _replacementsDictionary["$safeprojectname$"];

		public string ProjectName => _replacementsDictionary["$projectname$"];

		public string DestinationPath => new DirectoryInfo(_replacementsDictionary["$destinationdirectory$"]).FullName;

		public string GenerationOutputPath => DestinationPath;

		public ProjectInfo ProjectInfo { get; } = new ProjectInfo();

		public List<FailedMergePostActionInfo> FailedMergePostActions { get; } = new List<FailedMergePostActionInfo>();

		public Dictionary<string, List<MergeInfo>> MergeFilesFromProject { get; } = new Dictionary<string, List<MergeInfo>>();

		public List<string> FilesToOpen { get; } = new List<string>();

		public Dictionary<ProjectMetricsEnum, double> ProjectMetrics { get; private set; } = new Dictionary<ProjectMetricsEnum, double>();

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}








		public App()
		{
			Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());
			Resources.Add("HasItemsVisibilityConverter", new HasItemsVisibilityConverter());
			Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());

			Initialize();
		}
		public void Initialize()
		{

			//_platform = _replacementsDictionary.SafeGet("$ts.platform$");
			//_appModel = _replacementsDictionary.SafeGet("$ts.appmodel$");
			//_language = _replacementsDictionary.SafeGet("$ts.language$");
			_platform = "WinUI";
			_appModel = "Desktop";
			_language = "C#";
			TelemetryService.Current.WizardVersion = GenContext.GetWizardVersionFromAssembly().ToString();

			if (GenContext.CurrentLanguage != _language || GenContext.CurrentPlatform != _platform)
			{
				GenContext.Bootstrap(new VsixTemplatesSource(string.Empty, platform: _platform), new VsGenShell(), _platform, _language);
			}
			//GenContext.Current = new IContextProvider();

			var context = new UserSelectionContext(_language, _platform);
			if (!string.IsNullOrEmpty(_appModel))
			{
				context.AddAppModel(_appModel);
			}

			var requiredVersion = _replacementsDictionary.SafeGet("$ts.requiredversion$");
			var requiredworkloads = _replacementsDictionary.SafeGet("$ts.requiredworkloads$");

			//_userSelection = WizardLauncher.Instance.StartNewProject(context, requiredVersion, requiredworkloads, new VSStyleValuesProvider());


		}

	}
}
