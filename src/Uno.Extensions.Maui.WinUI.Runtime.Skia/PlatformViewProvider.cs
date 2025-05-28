using Uno.Extensions.Maui.Internals;
using Uno.Extensions.Maui.WinUI.Runtime.Skia;
using Uno.Foundation.Extensibility;

[assembly: ApiExtension(typeof(IPlatformViewProvider), typeof(PlatformViewProvider))]


namespace Uno.Extensions.Maui.WinUI.Runtime.Skia;

internal class PlatformViewProvider : IPlatformViewProvider
{
	public object? GetPlatformView(IMauiContext mauiContext) =>
		// ???
}
