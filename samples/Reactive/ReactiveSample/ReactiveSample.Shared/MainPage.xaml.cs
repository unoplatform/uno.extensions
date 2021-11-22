using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace ReactiveSample
{
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();
		}

		public ViewModel.BindableViewModel VM { get; } = new();
	}
}
