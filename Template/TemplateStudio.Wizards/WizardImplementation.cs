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
using TemplateStudio.Wizards.Views;
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


				// Display a form to the user. The form collects
				// input for the custom message.
				//inputForm = new UserInputForm();
				//inputForm.ShowDialog();

				var page1 = new MainPage();
				page1.Owner = Application.Current.MainWindow;
				page1.Focus();
				UI.ShowModal(page1);

				//Page1 window = new Page1();
				
				//window.ShowDialog();


				//replacementsDictionary.Add("passthrough:skia-gtk", page1.ContextViewModel.DataReplacement.skiaGtk.ToString());
				//replacementsDictionary.Add("passthrough:wasm", page1.ContextViewModel.DataReplacement.wasm.ToString());
				//replacementsDictionary.Add("passthrough:ios", page1.ContextViewModel.DataReplacement.ios.ToString());
				//replacementsDictionary.Add("passthrough:android", page1.ContextViewModel.DataReplacement.android.ToString());
				//replacementsDictionary.Add("passthrough:macos", page1.ContextViewModel.DataReplacement.macos.ToString());
				//replacementsDictionary.Add("passthrough:maccatalyst",page1.ContextViewModel.DataReplacement.maccatalyst.ToString());
				//replacementsDictionary.Add("passthrough:tests",page1.ContextViewModel.DataReplacement.tests.ToString());
				//replacementsDictionary.Add("passthrough:skia-wpf",page1.ContextViewModel.DataReplacement.skiaWpf.ToString());
				//replacementsDictionary.Add("passthrough:skia-linux-fb",page1.ContextViewModel.DataReplacement.skiaLinuxFb.ToString());
				//replacementsDictionary.Add("passthrough:winAppSdk",page1.ContextViewModel.DataReplacement.winAppSdk.ToString());
				//replacementsDictionary.Add("passthrough:reactive",page1.ContextViewModel.DataReplacement.reactive.ToString());
				//replacementsDictionary.Add("passthrough:cpm",page1.ContextViewModel.DataReplacement.cpm.ToString());
				//replacementsDictionary.Add("passthrough:wasm-pwa-manifest",page1.ContextViewModel.DataReplacement.wasmPpwaManifest.ToString());
				//replacementsDictionary.Add("passthrough:vscode",page1.ContextViewModel.DataReplacement.vscode.ToString());
				//replacementsDictionary.Add("passthrough:skipRestore",page1.ContextViewModel.DataReplacement.skiaGtk.ToString());


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
