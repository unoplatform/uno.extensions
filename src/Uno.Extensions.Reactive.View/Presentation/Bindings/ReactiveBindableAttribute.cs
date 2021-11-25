using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Reactive;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
internal class ReactiveBindableAttribute : Attribute
{
	public bool IsEnabled { get; }

	public ReactiveBindableAttribute(bool isEnabled = true)
	{
		IsEnabled = isEnabled;
	}
}
