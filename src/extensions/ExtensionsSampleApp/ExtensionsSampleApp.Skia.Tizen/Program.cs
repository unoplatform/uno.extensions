using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace ExtensionsSampleApp.Skia.Tizen
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new TizenHost(() => new ExtensionsSampleApp.App(), args);
            host.Run();
        }
    }
}
