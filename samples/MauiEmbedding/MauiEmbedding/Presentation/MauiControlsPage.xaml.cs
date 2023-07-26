using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Page = Microsoft.UI.Xaml.Controls.Page;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MauiEmbedding.Presentation;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MauiControlsPage : Page
{
	public MauiControlsPage()
	{
		this.InitializeComponent();

		var lbl = new Label()
		{
			BackgroundColor = Colors.Pink
		};

		var b = new BoxView
		{
			HeightRequest = 300,
			WidthRequest = 300
		};

		var source = new List<string> { "Uno", "Maui", "WinUI" };

		this.picker.ItemsSource = source;
		var mauiBinding = new Microsoft.Maui.Controls.Binding
		{
			Path = nameof(MauiControlsViewModel.Name),
			Source = DataContext
		};

		lbl.SetBinding(Label.TextProperty, mauiBinding);

		this.stack.Add(lbl);
		this.stack.Add(b);

		this.lblB.BindingContextChanged += (s, e) =>
		{
			_ = 1;
			_ = this.lblB.Text;
			this.lblB.HeightRequest = 30;
			_ = this.lblB.IsSet(Label.TextProperty);
		};

		this.lblB.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == "Text")
			{
				_ = 1;
			}
		};

		//lblB.BackgroundColor = Microsoft.Maui.Graphics.Colors.Black;

		this.mauiContent.DataContextChanged += (s, e) =>
		{
			_ = 1;
		};

		this.lblB.HeightRequest = 20;

		this.DataContextChanged += (s, e) =>
		{
			_ = 1;
		};

	}
}
