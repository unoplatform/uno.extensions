namespace Uno.Extensions.Maui.Interop;

public interface IWinUIToMauiStyleHandler
{
	Type TargetType { get; }

	(Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value);
}
