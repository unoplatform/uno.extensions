namespace Uno.Extensions.Maui;

public class VisualElementChangedEventArgs : EventArgs
{
	public VisualElementChangedEventArgs(VisualElement content)
	{
		Content = content;
	}

	public VisualElement Content { get; }
}
