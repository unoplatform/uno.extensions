using System;
using System.Linq;
using System.Windows;
using Uno.UI.Runtime.Skia.Wpf;

namespace RuntimeTests.WPF;

public partial class App : Application
{
	public App()
	{
		new WpfHost(Dispatcher, () => new Uno.Extensions.RuntimeTests.App()).Run();
	}
}
