using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TemplateStudio.Wizards.Model;
using TemplateStudio.Wizards.ViewModel;

using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uno.UI.XamlHost.Skia.Wpf;

namespace TemplateStudio.Wizards.Host
{
	public partial class WizardHost : Window
	{
		public ContextViewModel ContextViewModel = null;
		public Grid RootGrid { get; private set; }

		public CheckBox CheckBoxSkiaGtk { get; private set; }
		public CheckBox CheckBoxWasm { get; private set; }
		public CheckBox CheckBoxIos { get; private set; }
		public CheckBox CheckBoxAndroid { get; private set; }
		public CheckBox CheckBoxMacos { get; private set; }
		public CheckBox CheckBoxMaccatalyst { get; private set; }
		public CheckBox CheckBoxTests { get; private set; }
		public CheckBox CheckBoxSkiaWpf { get; private set; }
		public CheckBox CheckBoxSkiaLinuxFb { get; private set; }
		public CheckBox CheckBoxWinAppSdk { get; private set; }
		public RadioButton CheckBoxReactive { get; private set; }
		public CheckBox CheckBoxCpm { get; private set; }
		public CheckBox CheckBoxWasmPpwaManifest { get; private set; }
		public CheckBox CheckBoxVscode { get; private set; }
		public CheckBox CheckBoxSkipRestore { get; private set; }
		public WizardHost()
		{
			//Initialize();
			//DataContext = this;
			//ContextViewModel.DataReplacement.android = true;
			InitializeComponent();

			ContextViewModel = new ContextViewModel();
			this.Content = new UnoXamlHost() { InitialTypeName = "TemplateStudio.Wizards.MainUnoPage", Height = 1024, Width = 786 };
		}
		protected void Initialize()
		{
			ContextViewModel = new ContextViewModel();

			//this.WindowStyle = WindowStyle.ThreeDBorderWindow;

			//this.RootGrid = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

			//this.RootGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(800) });
			//this.RootGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(50) });
			//this.RootGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(500) });

			//StackPanel StackPanelContainer = new StackPanel();
			//StackPanelContainer.Margin = new Thickness(30, 0, 30, 0);

			//StackPanel StackPanel = new StackPanel();


			//StackPanel.Children.Add(new Label() { Content = "Platform", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//StackPanel.Children.Add(new Label() { Content = "Mobile", FontSize = 12, Margin = new Thickness(0, 10, 0, 0) });
			//CheckBoxIos = new CheckBox() { Name = "Ios", IsChecked = true, Content = "IOS" };
			//CheckBoxAndroid = new CheckBox() { Name = "Android", IsChecked = true, Content = "Android" };

			//StackPanel.Children.Add(CheckBoxIos);
			//StackPanel.Children.Add(CheckBoxAndroid);

			//StackPanel.Children.Add(new Label() { Content = "Desktop", FontSize = 12, Margin = new Thickness(0, 10, 0, 0) });
			//CheckBoxSkiaWpf = new CheckBox() { Name = "SkiaWpf", IsChecked = true, Content = "Skia Wpf" };
			//CheckBoxSkiaLinuxFb = new CheckBox() { Name = "LinuxFb", IsChecked = true, Content = "Linux Fb" };
			//CheckBoxWinAppSdk = new CheckBox() { Name = "AppSdk", IsChecked = true, Content = "App Sdk" };
			//CheckBoxSkiaGtk = new CheckBox() { Name = "skiaGtk", IsChecked = true, Content = "skia Gtk" };
			//CheckBoxMacos = new CheckBox() { Name = "Macos", IsChecked = true, Content = "MacOS" };
			//CheckBoxMaccatalyst = new CheckBox() { Name = "Maccatalyst", IsChecked = true, Content = "Mac Catalyst" };

			//StackPanel.Children.Add(CheckBoxSkiaWpf);
			//StackPanel.Children.Add(CheckBoxSkiaGtk);
			//StackPanel.Children.Add(CheckBoxMacos);
			//StackPanel.Children.Add(CheckBoxMaccatalyst);
			//StackPanel.Children.Add(CheckBoxSkiaLinuxFb);
			//StackPanel.Children.Add(CheckBoxWinAppSdk);



			//StackPanel.Children.Add(new Label() { Content = "Web", FontSize = 12, Margin = new Thickness(0, 10, 0, 0) });
			//CheckBoxWasm = new CheckBox() { Name = "Wasm", IsChecked = true, Content = "Wasm" };
			//StackPanel.Children.Add(CheckBoxWasm);

			//StackPanel.Children.Add(new Label() { Content = "Features", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//CheckBoxTests = new CheckBox() { Name = "Tests", IsChecked = true, Content = "Tests" };
			//CheckBoxWasmPpwaManifest = new CheckBox() { Name = "WasmPpwaManifest", IsChecked = true, Content = "Wasm Ppwa Manifest" };
			//CheckBoxVscode = new CheckBox() { Name = "Vscode", IsChecked = true, Content = "Vscode" };
			//CheckBoxSkipRestore = new CheckBox() { Name = "SkipRestore", IsChecked = true, Content = "Skip Restore" };

			//StackPanel.Children.Add(CheckBoxTests);
			//StackPanel.Children.Add(CheckBoxWasmPpwaManifest);
			//StackPanel.Children.Add(CheckBoxVscode);
			//StackPanel.Children.Add(CheckBoxSkipRestore);

			//StackPanel.Children.Add(new Label() { Content = "Extensions", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//CheckBoxCpm = new CheckBox() { Name = "Cpm", IsChecked = true, Content = "Cpm" };
			//StackPanel.Children.Add(new CheckBox() { Name = "Configuration", IsChecked = true, Content = "Configuration(appsettings.json)" });
			//StackPanel.Children.Add(new CheckBox() { Name = "Logging", IsChecked = true, Content = "Logging" });
			//StackPanel.Children.Add(new CheckBox() { Name = "Serilog", IsChecked = true, Content = "Logging using Serilog" });
			//StackPanel.Children.Add(new CheckBox() { Name = "Localization", IsChecked = true, Content = "Localization" });
			//StackPanel.Children.Add(new CheckBox() { Name = "Navigation", IsChecked = true, Content = "Navigation" });

			//StackPanel.Children.Add(CheckBoxCpm);

			//StackPanel.Children.Add(new Label() { Content = "Coding Style", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//StackPanel.Children.Add(new RadioButton() { Name = "XAML", GroupName = "CodingStyle", IsChecked = true, Content = "XAML" });
			//StackPanel.Children.Add(new RadioButton() { Name = "Markup", GroupName = "CodingStyle", Content = "C# Markup" });


			//StackPanel.Children.Add(new Label() { Content = "Framework", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//StackPanel.Children.Add(new RadioButton() { Name = "net6", GroupName = "Framework", IsChecked = true, Content = "net6" });
			//StackPanel.Children.Add(new RadioButton() { Name = "net7", GroupName = "Framework", Content = "net7" });


			//StackPanel.Children.Add(new Label() { Content = "Architecture", FontSize = 14, Margin = new Thickness(0, 10, 0, 0) });
			//StackPanel.Children.Add(new RadioButton() { Name = "Mvvm", GroupName = "Architecture", IsChecked = true, Content = "Mvvm (CommunityToolkit.mvvm) " });
			//CheckBoxReactive = new RadioButton() { Name = "MVU", GroupName = "Architecture", Content = "MVU-X (Uno.Extensions.Reactive) " };
			//StackPanel.Children.Add(CheckBoxReactive);


			//ScrollViewer ScrollViewer = new ScrollViewer();
			//ScrollViewer.CanContentScroll = true;
			//ScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			//ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			//ScrollViewer.Content = StackPanel;
			//StackPanelContainer.Children.Add(ScrollViewer);
			//Grid.SetColumn(StackPanelContainer, 0);
			//Grid.SetRow(StackPanelContainer, 0);
			//this.RootGrid.Children.Add(StackPanelContainer);


			//Grid GridForButtons = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			//Button Button_Close = new Button() { Content = "Generate" };
			//Button_Close.Click += Button_Close_Click;

			//Grid.SetColumn(Button_Close, 0);
			//Grid.SetRow(Button_Close, 0);
			//GridForButtons.Children.Add(Button_Close);


			//Grid.SetRow(GridForButtons, 1);
			//Grid.SetColumn(GridForButtons, 0);
			//this.RootGrid.Children.Add(GridForButtons);

			//this.Content = this.RootGrid;
			//this.Content = new UnoXamlHost() { InitialTypeName = "TemplateStudio.Wizards.MainUnoPage", Height = 500, Width = 500 };


			var grid = new Grid { Height = 500, Width = 500 };
			var butt = new Button { Content = "ClickEd" };
			butt.Click += Butt_Click;
			grid.Children.Add(butt);
			this.Content = grid;
			this.SizeToContent = SizeToContent.WidthAndHeight;
			//this.Loaded += Butt_Click;
		}

		private void Butt_Click(object sender, RoutedEventArgs e)
		{
			this.Content = new UnoXamlHost() { InitialTypeName="TemplateStudio.Wizards.MainUnoPage", Height=1024,Width=786 };

			this.SizeToContent = SizeToContent.WidthAndHeight;
		}

		private void Button_Close_Click(object sender, RoutedEventArgs e)
		{

			this.ContextViewModel.DataReplacement.skiaGtk = this.CheckBoxSkiaGtk.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.wasm = this.CheckBoxWasm.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.ios = this.CheckBoxIos.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.android = this.CheckBoxAndroid.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.macos = this.CheckBoxMacos.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.maccatalyst = this.CheckBoxMaccatalyst.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.tests = this.CheckBoxTests.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.skiaWpf = this.CheckBoxSkiaWpf.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.skiaLinuxFb = this.CheckBoxSkiaLinuxFb.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.winAppSdk = this.CheckBoxWinAppSdk.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.reactive = this.CheckBoxReactive.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.cpm = this.CheckBoxCpm.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.wasmPpwaManifest = this.CheckBoxWasmPpwaManifest.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.vscode = this.CheckBoxVscode.IsChecked.GetValueOrDefault(false);
			this.ContextViewModel.DataReplacement.skipRestore = this.CheckBoxSkipRestore.IsChecked.GetValueOrDefault(false);
			this.Close();
		}
	}
}
