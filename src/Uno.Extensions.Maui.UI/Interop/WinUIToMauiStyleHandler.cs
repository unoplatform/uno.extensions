using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui.Interop;

public abstract class WinUIToMauiStyleHandler : IWinUIToMauiStyleHandler
{
	public abstract Type TargetType { get; }

	public abstract (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value);

	protected virtual object? ConvertToMauiValue(object value)
	{
		return ConversionHelpers.ToMauiValue(value);
	}

	protected bool TryConvertValue(object value, out object? output)
	{
		try
		{
			output = ConvertToMauiValue(value);
			return output != value;
		}
		catch
		{
			output = null;
			return false;
		}
	}
}
