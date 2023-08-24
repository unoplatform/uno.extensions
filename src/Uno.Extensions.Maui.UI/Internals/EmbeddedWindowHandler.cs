namespace Uno.Extensions.Maui.Internals;

internal class EmbeddedWindowHandler : IElementHandler
{
	public object? PlatformView { get; set; }
	public IElement? VirtualView { get; set; }
	public IMauiContext? MauiContext { get; set; }

	public void DisconnectHandler() { }
	public void Invoke(string command, object? args = null) { }
	public void SetMauiContext(IMauiContext mauiContext) => MauiContext = mauiContext;
	public void SetVirtualView(IElement view) => VirtualView = view;
	public void UpdateValue(string property) { }
}
