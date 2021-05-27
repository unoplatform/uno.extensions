using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace ApplicationTemplate.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new ApplicationTemplate.App(), args);
		host.Run();
	}
}
}
