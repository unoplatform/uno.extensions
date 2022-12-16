using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using EnvDTE;

namespace TemplateStudio.Wizards.ViewModels
{
	internal class MainViewModel : MainViewModelBase
	{
		private string unoCheck;
		public string Test => "Hello Wizard!";
		public Dictionary<string, string> Replacements { get; set; }
		public string UnoCheck {
			get => unoCheck;
			set {set(ref unoCheck, value);}
		}
	}
}
