namespace Uno.Extensions.Maui.Interop;

internal interface IWinUIToMauiStyleHandler
{
	Type TargetType { get; }

	MauiToWinUIStyleMapping? Process(DependencyProperty property, object value);
}
