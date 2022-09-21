using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

[AttributeUsage(AttributeTargets.Parameter)]
internal class ContextTypeAttribute : Attribute
{
	public string Type { get; set; }

	public ContextTypeAttribute(Type type)
	{
		Type = type.FullName;
	}

	public ContextTypeAttribute(string type)
	{
		Type = type.StartsWith("global::", StringComparison.OrdinalIgnoreCase)
			? type.Substring("global::".Length)
			: type;
	}
}
