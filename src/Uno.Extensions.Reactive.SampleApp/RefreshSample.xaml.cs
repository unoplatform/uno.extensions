using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Uno.Extensions.Reactive.SampleApp;

public sealed partial class RefreshSample : Page
{
	public RefreshSample()
	{
		this.InitializeComponent();

		DataContext = new RefreshSampleViewModel.BindableRefreshSampleViewModel();
	}
}
