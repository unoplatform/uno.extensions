using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace TemplateTesteWPF.Helpers
{
	public static class UI
	{
		public static void ShowModal(Window shell)
		{
			if (shell is Window dialog)
			{
				dialog.Owner = Application.Current.MainWindow;
				dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
				dialog.ShowDialog();
				
				if (dialog.WindowState == WindowState.Minimized)
				{
					dialog.WindowState = WindowState.Normal;
				}
				dialog.Activate();
				dialog.Topmost = true;  // important
				dialog.Topmost = false; // important
				dialog.Focus();         // important
			}
		}
		
	}
}
