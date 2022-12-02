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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TemplateTesteWPF
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        public Page1()
        {
            InitializeComponent();
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            //if (stepFrame.Content == null)
            //{
            //    Services.NavigationService.InitializeSecondaryFrame(stepFrame, WizardNavigation.Current.CurrentStep.GetPage());
            //    sequentialFlow.FocusFirstStep();
            //}

            //if (_focusedElement != null)
            //{
            //    _focusedElement.Focus();
            //    Keyboard.Focus(_focusedElement);
            //}

            //Services.NavigationService.SubscribeEventHandlers();
            WizardNavigation.Current.SubscribeEventHandlers();
            PreviewGotKeyboardFocus += OnPreviewGotKeyboardFocus;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            //Services.NavigationService.UnsubscribeEventHandlers();
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
            //if (sender is System.Windows.Controls.ListView listView)
            //{
            //    if (listView.Tag is TemplateType templateType)
            //    {
            //        var group = MainViewModel.Instance.UserSelection.Groups.First(g => g.TemplateType == templateType);
            //        group.EnableOrdering(listView);
            //    }
            //}
        }

        private void OnPreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // _focusedElement = e.NewFocus;
        }
    }
}
