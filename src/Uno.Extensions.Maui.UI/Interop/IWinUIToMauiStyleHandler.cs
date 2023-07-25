namespace Uno.Extensions.Maui.Interop;

public interface IWinUIToMauiStyleHandler
{
	Type TargetType { get; }

	MauiToWinUIStyleMapping? Process(DependencyProperty property, object value);
}
