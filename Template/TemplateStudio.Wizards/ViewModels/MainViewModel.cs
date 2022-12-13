using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplateStudio.Wizards.ViewModels
{
	internal class MainViewModel
	{
		public string Test => "Hello Wizard!";
		public Dictionary<string, string> Replacements { get; set; }

		
	}
}
