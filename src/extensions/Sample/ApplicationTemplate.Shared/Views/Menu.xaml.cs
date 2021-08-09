using System.Linq;
using Windows.UI.ViewManagement;
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using ApplicationTemplate.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

namespace ApplicationTemplate.Views.Content
{
    public sealed partial class Menu : AttachableUserControl
    {
        public Menu()
        {
            this.InitializeComponent();
            Loaded += MenuLoaded;
        }

        private void MenuLoaded(object sender, RoutedEventArgs e)
        {
            InitializeSafeArea();
        }

        /// <summary>
        /// This method handles the bottom padding for phones like iPhone X.
        /// </summary>
        private void InitializeSafeArea()
        {
            var full = (App.Current as App).CurrentWindow.Bounds;
            var bounds = full;
            try
            {
                bounds = ApplicationView.GetForCurrentView().VisibleBounds;

            }
            catch
            {
                // Ignore this and just use the default value (ie full)
            }

            var bottomPadding = full.Bottom - bounds.Bottom;

            if (bottomPadding > 0)
            {
                SafeAreaRow.Height = new GridLength(bottomPadding);
            }

            var totalHeight = MenuRoot.RowDefinitions.Sum(rd => rd.Height.Value);
            CloseTranslateAnimation.To = totalHeight;
            MenuTranslateTransform.Y = totalHeight;
        }
    }
}


namespace ApplicationTemplate.Views
{ 
    public class BindableVisualState
    {
        public static readonly DependencyProperty VisualStateNameProperty = DependencyProperty.RegisterAttached("VisualStateName", typeof(string), typeof(BindableVisualState), new PropertyMetadata(null, OnVisualStateNameChanged));

        public static readonly DependencyProperty VisualStateName2Property = DependencyProperty.RegisterAttached("VisualStateName2", typeof(string), typeof(BindableVisualState), new PropertyMetadata(null, OnVisualStateNameChanged));

        public static readonly DependencyProperty VisualStateName3Property = DependencyProperty.RegisterAttached("VisualStateName3", typeof(string), typeof(BindableVisualState), new PropertyMetadata(null, OnVisualStateNameChanged));

        public static readonly DependencyProperty VisualStateName4Property = DependencyProperty.RegisterAttached("VisualStateName4", typeof(string), typeof(BindableVisualState), new PropertyMetadata(null, OnVisualStateNameChanged));

        public static string GetVisualStateName(DependencyObject obj)
        {
            return (string)obj.GetValue(VisualStateNameProperty);
        }

        public static void SetVisualStateName(DependencyObject obj, string value)
        {
            obj.SetValue(VisualStateNameProperty, value);
        }

        private static void OnVisualStateNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Control control = (Control)d;
            string text = (string)e.NewValue;
            if (control != null && text != null)
            {
                VisualStateManager.GoToState(control, text, useTransitions: true);
            }
        }

        public static string GetVisualStateName2(DependencyObject obj)
        {
            return (string)obj.GetValue(VisualStateName2Property);
        }

        public static void SetVisualStateName2(DependencyObject obj, string value)
        {
            obj.SetValue(VisualStateName2Property, value);
        }

        public static string GetVisualStateName3(DependencyObject obj)
        {
            return (string)obj.GetValue(VisualStateName3Property);
        }

        public static void SetVisualStateName3(DependencyObject obj, string value)
        {
            obj.SetValue(VisualStateName3Property, value);
        }

        public static string GetVisualStateName4(DependencyObject obj)
        {
            return (string)obj.GetValue(VisualStateName4Property);
        }

        public static void SetVisualStateName4(DependencyObject obj, string value)
        {
            obj.SetValue(VisualStateName4Property, value);
        }
    }
}
