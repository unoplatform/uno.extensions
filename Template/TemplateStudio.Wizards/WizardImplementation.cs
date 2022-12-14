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
using TemplateStudio.Wizards.ViewModels;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace TemplateStudio.Wizards
{
	public class WizardImplementation : IWizard
	{
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
				//Get Uno Check Result File
				Helpers.ProcessCommand.getUnoCheck();


				var host = new WizardHost();
				host.DataContext = new MainViewModel() { Replacements = replacementsDictionary };
				UI.ShowModal(host);

				replacementsDictionary.Add("passthrough:is-visx", true.ToString());
				MessageBox.Show("Wizard done!");

				//Validate on screen
				MessageBox.Show(
					string.Join(",",
					replacementsDictionary.Skip(28).Select(d => string.Format("\"{0}\": [{1}]", d.Key, string.Join(",", d.Value)))
					));

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
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
