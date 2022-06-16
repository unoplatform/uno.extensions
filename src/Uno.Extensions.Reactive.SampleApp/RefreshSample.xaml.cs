using System;
using System.Linq;
using Windows.UI.Xaml.Controls;

namespace Uno.Extensions.Reactive.SampleApp;

public sealed partial class RefreshSample : Windows.UI.Xaml.Controls.Page
{
	public RefreshSample()
	{
		this.InitializeComponent();

		DataContext = new RefreshSampleViewModel.BindableRefreshSampleViewModel();
	}
}
