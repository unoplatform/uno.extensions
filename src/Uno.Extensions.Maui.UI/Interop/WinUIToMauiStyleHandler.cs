﻿using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui.Interop;

internal abstract class WinUIToMauiStyleHandler : IWinUIToMauiStyleHandler
{
	public abstract Type TargetType { get; }

	public abstract MauiToWinUIStyleMapping? Process(DependencyProperty property, object value);

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
