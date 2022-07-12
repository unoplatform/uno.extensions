using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace TestHarness.Legacy.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new TestHarness.Legacy.App());
		host.Run();
	}
}
}
