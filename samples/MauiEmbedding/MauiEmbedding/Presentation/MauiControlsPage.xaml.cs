using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Maui.Controls;
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
		//DataContext = new MauiControlsViewModel();
		var lbl = new Label();

		var mauiBinding = new Microsoft.Maui.Controls.Binding
		{
			Path = "Title",
			Source = DataContext
		};

		lbl.SetBinding(Label.TextProperty, mauiBinding);

		this.stack.Add(lbl);
	}
}
