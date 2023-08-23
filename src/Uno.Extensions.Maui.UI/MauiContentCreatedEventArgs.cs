namespace Uno.Extensions.Maui;

public class MauiContentCreatedEventArgs : EventArgs
{
	public MauiContentCreatedEventArgs(VisualElement content)
	{
		Content = content;
	}

	public VisualElement Content { get; }
}
