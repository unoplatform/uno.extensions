using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateStudio.Wizards.Helpers;
using TemplateStudio.Wizards.Model;

namespace TemplateStudio.Wizards.ViewModel
{
	public class ContextViewModel : Observable
	{
		private DataReplacement _dataReplacement;
		public ContextViewModel() {
			_dataReplacement = new DataReplacement();	
		}

		public DataReplacement DataReplacement
		{
			get => _dataReplacement;
			private set => SetProperty(ref _dataReplacement, value);
		}

	}
}
