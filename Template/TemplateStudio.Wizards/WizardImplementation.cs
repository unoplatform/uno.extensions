using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TemplateWizard;
//using System.Windows.Forms;
using EnvDTE;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TemplateStudio.Wizards.Host;
using Microsoft.VisualStudio.Telemetry;
using TemplateStudio.Wizards.Helpers;
using TemplateStudio.Wizards.Model;
using TemplateStudio.Wizards.ViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace TemplateStudio.Wizards
{
	public class WizardImplementation : IWizard
	{
		private string customMessage;

		// This method is called before opening any item that
		// has the OpenInEditor attribute.
		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		// This method is only called for item templates,
		// not for project templates.
		public void ProjectItemFinishedGenerating(ProjectItem
			projectItem)
		{
		}

		// This method is called after the project is created.
		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject,
			Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{
			try
			{
				var host = new WizardHost();
				host.Focus();
				UI.ShowModal(host);

				replacementsDictionary.Add("passthrough:skia-gtk", host.ContextViewModel.DataReplacement.skiaGtk.ToString());
				replacementsDictionary.Add("passthrough:wasm", host.ContextViewModel.DataReplacement.wasm.ToString());
				replacementsDictionary.Add("passthrough:ios", host.ContextViewModel.DataReplacement.ios.ToString());
				replacementsDictionary.Add("passthrough:android", host.ContextViewModel.DataReplacement.android.ToString());
				replacementsDictionary.Add("passthrough:macos", host.ContextViewModel.DataReplacement.macos.ToString());
				replacementsDictionary.Add("passthrough:maccatalyst", host.ContextViewModel.DataReplacement.maccatalyst.ToString());
				replacementsDictionary.Add("passthrough:tests", host.ContextViewModel.DataReplacement.tests.ToString());
				replacementsDictionary.Add("passthrough:skia-wpf", host.ContextViewModel.DataReplacement.skiaWpf.ToString());
				replacementsDictionary.Add("passthrough:skia-linux-fb", host.ContextViewModel.DataReplacement.skiaLinuxFb.ToString());
				replacementsDictionary.Add("passthrough:winAppSdk", host.ContextViewModel.DataReplacement.winAppSdk.ToString());
				replacementsDictionary.Add("passthrough:reactive", host.ContextViewModel.DataReplacement.reactive.ToString());
				replacementsDictionary.Add("passthrough:wasm-pwa-manifest", host.ContextViewModel.DataReplacement.wasmPpwaManifest.ToString());
				replacementsDictionary.Add("passthrough:vscode", host.ContextViewModel.DataReplacement.vscode.ToString());


				//replacementsDictionary.Add("passthrough:cpm", host.ContextViewModel.DataReplacement.cpm.ToString());
				//replacementsDictionary.Add("passthrough:skipRestore", false.ToString());
				replacementsDictionary.Add("passthrough:is-visx", true.ToString());


			}
			catch (Exception ex)
			{
				//MessageBox.Show(ex.ToString());
			}
		}

		// This method is only called for item templates,
		// not for project templates.
		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}


	}
}
