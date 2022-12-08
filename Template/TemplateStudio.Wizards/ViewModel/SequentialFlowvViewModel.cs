using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using TemplateStudio.Wizards.Model;
using TemplateStudio.Wizards;

namespace TemplateStudio.Wizards.ViewModel
{
	public class SequentialFlowvViewModel
	{
		public Frame ContentFrame { get; set; }
		public ObservableCollection< SequentialFlow> SequentialFlowList { get; set; }
		private SequentialFlow _SelectSequentialFlow { get; set; }
		public SequentialFlow SelectSequentialFlow {
			get { return _SelectSequentialFlow; }
			set {
				if (_SelectSequentialFlow != value) { _SelectSequentialFlow = value; HandleSelectedItem(); }
			}
		}
		public void HandleSelectedItem()
		{
			if(ContentFrame != null)
			{ 
				//ContentFrame.Content = SelectSequentialFlow.getPage;
			}
		}
		public SequentialFlowvViewModel() {
			SequentialFlowList = new ObservableCollection<SequentialFlow>()
			{
				new SequentialFlow() { Name = "Platform" },
				new SequentialFlow() { Name = "Features" },
				new SequentialFlow() { Name = "Extensions" },
				new SequentialFlow() { Name = "Coding Style" },
				new SequentialFlow() { Name = "Framework" },
				new SequentialFlow() { Name = "Architecture" }

			};
		}
	}
}
