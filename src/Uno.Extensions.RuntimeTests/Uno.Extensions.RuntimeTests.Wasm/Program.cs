using System;
using Microsoft.UI.Xaml;
using RuntimeTests;

namespace Uno.Extensions.RuntimeTests.Wasm
{
	public class Program
	{
		private static App _app = default!;

		static int Main(string[] args)
		{
			Microsoft.UI.Xaml.Application.Start(_ => _app = new App());

			return 0;
		}
	}
}
