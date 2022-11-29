using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
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

namespace TemplateStudio.Wizards
{
	public class WizardImplementation : IWizard
	{
		private UserInputForm inputForm;
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
				inputForm = new UserInputForm();
				inputForm.ShowDialog();

				customMessage = UserInputForm.CustomMessage;

				Application app = new Application();
				app.Run(new Page1());
				new Page1();
				
				replacementsDictionary.Add("passthrough:skia-gtk", true.ToString());
				replacementsDictionary.Add("passthrough:wasm", false.ToString());
				replacementsDictionary.Add("passthrough:ios", false.ToString());
				replacementsDictionary.Add("passthrough:android", true.ToString());
				replacementsDictionary.Add("passthrough:macos", false.ToString());
				replacementsDictionary.Add("passthrough:maccatalyst", false.ToString());
				replacementsDictionary.Add("passthrough:tests", false.ToString());
				replacementsDictionary.Add("passthrough:skia-wpf", false.ToString());
				replacementsDictionary.Add("passthrough:skia-linux-fb", false.ToString());
				replacementsDictionary.Add("passthrough:winAppSdk", false.ToString());
				replacementsDictionary.Add("passthrough:reactive", false.ToString());
				replacementsDictionary.Add("passthrough:cpm", false.ToString());
				replacementsDictionary.Add("passthrough:wasm-pwa-manifest", false.ToString());
				replacementsDictionary.Add("passthrough:vscode", false.ToString());
				replacementsDictionary.Add("passthrough:skipRestore", false.ToString());


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
