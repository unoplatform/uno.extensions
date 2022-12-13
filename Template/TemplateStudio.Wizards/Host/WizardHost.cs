using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TemplateStudio.Wizards.Model;
using TemplateStudio.Wizards.ViewModels;

using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uno.UI.XamlHost.Skia.Wpf;

namespace TemplateStudio.Wizards.Host
{
	public partial class WizardHost : Window
	{
		public WizardHost(Dictionary<string, string> replacementsDictionary)
		{
			this.Content = new UnoXamlHost() {
				InitialTypeName = "TemplateStudio.Wizards.MainUnoPage",
				Width = 786,
				Height = 1024,
				DataContext = new MainViewModel() { Replacements = replacementsDictionary }
			};
		}
	}
}
