using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace Playground.Skia.Tizen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var host = new TizenHost(() => new Playground.App(), args);
            host.Run();
        }
    }
}
