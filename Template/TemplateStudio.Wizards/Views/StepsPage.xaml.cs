using Microsoft.Templates.Core.Gen;
using Microsoft.Templates.Core;
using Microsoft.Templates.UI.Controls;
using Microsoft.Templates.UI.Converters;
using Microsoft.Templates.UI.Styles;
using Microsoft.Templates.UI.ViewModels.Common;
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
using TemplateStudio.Wizards;
using TemplateStudio.Wizards.Views;
using Microsoft.Templates.UI.Services;
using Microsoft.Templates.UI.ViewModels.NewProject;
using Microsoft.Templates.UI.Views.NewProject;


namespace TemplateStudio.Wizards.Views
{
	/// <summary>
	/// Interaction logic for StepsPage.xaml
	/// </summary>
	public partial class StepsPage : Page
	{
		private IInputElement _focusedElement;
		public StepsPage()
		{
			DataContext = MainViewModel.Instance;

			Resources.MergedDictionaries.Add(AllStylesDictionary.GetMergeDictionary());
			Resources.Add("HasItemsVisibilityConverter", new HasItemsVisibilityConverter());
			Resources.Add("BoolToVisibilityConverter", new BoolToVisibilityConverter());

			InitializeComponent();
		}
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (stepFrame.Content == null)
			{
				Microsoft.Templates.UI.Services.NavigationService.InitializeSecondaryFrame(stepFrame, WizardNavigation.Current.CurrentStep.GetPage());
				sequentialFlow.FocusFirstStep();
			}

			if (_focusedElement != null)
			{
				_focusedElement.Focus();
				Keyboard.Focus(_focusedElement);
			}

			Microsoft.Templates.UI.Services.NavigationService.SubscribeEventHandlers();
			WizardNavigation.Current.SubscribeEventHandlers();
			PreviewGotKeyboardFocus += OnPreviewGotKeyboardFocus;
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			Microsoft.Templates.UI.Services.NavigationService.UnsubscribeEventHandlers();
			WizardNavigation.Current.UnsubscribeEventHandlers();
			PreviewGotKeyboardFocus -= OnPreviewGotKeyboardFocus;
		}

		private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(sender is ComboBox comboBox))
			{
				return;
			}

			if (e.Key == Key.Space)
			{
				comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
			}

			if (comboBox != null && !comboBox.IsDropDownOpen)
			{
				if (e.Key == Key.Left
					|| e.Key == Key.Up
					|| e.Key == Key.Right
					|| e.Key == Key.Down)
				{
					e.Handled = true;
				}
			}
		}

		private void UserSelectionGroupLoaded(object sender, RoutedEventArgs e)
		{
			if (sender is System.Windows.Controls.ListView listView)
			{
				if (listView.Tag is TemplateType templateType)
				{
					var group = MainViewModel.Instance.UserSelection.Groups.First(g => g.TemplateType == templateType);
					group.EnableOrdering(listView);
				}
			}
		}

		private void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			_focusedElement = e.NewFocus;
		}
	}
}
