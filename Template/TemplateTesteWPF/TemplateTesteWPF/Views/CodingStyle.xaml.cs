using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Templates.UI.Styles;
using Microsoft.Templates.UI.ViewModels.NewProject;

namespace TemplateTesteWPF.Views
{
	/// <summary>
	/// Interaction logic for CodingStyle.xaml
	/// </summary>
	public partial class CodingStyle : Page
	{
		public CodingStyle()
		{
			Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());

			DataContext = MainViewModel.Instance;
			InitializeComponent();
		}
	}
}
