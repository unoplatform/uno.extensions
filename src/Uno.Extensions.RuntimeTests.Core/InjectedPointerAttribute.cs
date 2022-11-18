using System;
using System.Linq;
using Windows.Devices.Input;

namespace Uno.UI.RuntimeTests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InjectedPointerAttribute : Attribute
{
	public PointerDeviceType Type { get; }

	public InjectedPointerAttribute(PointerDeviceType type)
	{
		Type = type;
	}
}
