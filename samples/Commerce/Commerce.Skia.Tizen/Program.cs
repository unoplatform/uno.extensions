using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace Commerce.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new Commerce.App(), args);
		host.Run();
	}
}
}
