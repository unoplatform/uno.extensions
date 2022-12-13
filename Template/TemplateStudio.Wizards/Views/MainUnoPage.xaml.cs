using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TemplateStudio.Wizards.ViewModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TemplateStudio.Wizards
{
	public partial class MainUnoPage : Page
	{
		SequentialFlowvViewModel sequentialFlowvViewModel = null;
		public MainUnoPage()
		{
			sequentialFlowvViewModel = new SequentialFlowvViewModel();
			DataContext = this;
			//stepFrame.Content = new SelectPage();
			this.InitializeComponent();
		}
	}
}
