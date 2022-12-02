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
using System.Windows.Shapes;
using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.UI.Controls;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.Styles;
using Microsoft.Templates.UI.Views.Common;
using Microsoft.Templates.UI.Views.NewProject;
using Microsoft.Templates.UI.ViewModels.NewProject;
using Microsoft.Templates.Core;

namespace TemplateTesteWPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		
		private readonly UserSelectionContext _context;

		public static MainWindow Current { get; private set; }

		public UserSelection Result { get; set; }

		public MainViewModel ViewModel { get; }

		public MainWindow()
		{
			Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());

			BaseStyleValuesProvider provider = new VSStyleValuesProvider();
			Current = this;
			ViewModel = new MainViewModel(this, provider);
			DataContext = ViewModel;

			var context = new UserSelectionContext(GenContext.CurrentLanguage, Platforms.WinUI);
			_context = context;
			MainViewModel.Instance.Initialize(context);
			InitializeComponent();
			NavigationService.InitializeMainFrame(mainFrame, new NewProjectMainPage());
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
				return;
			}

			if (e.Key == Key.Back
				&& NavigationService.CanGoBackMainFrame
				&& sender is MainWindow shell
				&& shell.mainFrame.NavigationService.Content is TemplateInfoPage)
			{
				NavigationService.GoBackMainFrame();
			}
		}

		private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			OnMouseLeftButtonDown(e);
			DragMove();
		}

#pragma warning disable VSTHRD100 // Avoid async void methods
		private async void OnLoaded(object sender, RoutedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
		{
			MainViewModel.Instance.Initialize(_context);
			////await MainViewModel.Instance.SynchronizeAsync();
			await MainViewModel.Instance.OnTemplatesAvailableAsync();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			ViewModel.UnsubscribeEventHandlers();
			NotificationsControl.UnsubscribeEventHandlers();
		}
	}
}
