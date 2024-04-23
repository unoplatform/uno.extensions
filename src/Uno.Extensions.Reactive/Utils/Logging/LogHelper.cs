using System;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Logging;

internal static class LogHelper
{
	public static string GetIdentifier(object? obj)
		=> obj switch
		{
			null => "--null--",
#if DEBUG
			_ => $"{GetTypeName(obj)}-{obj.GetHashCode():X8}",
#else
			_ => obj.ToString()
#endif
		};

	public static string GetTypeName(object obj)
		=> obj.GetType() switch
		{
			{ IsGenericType: true } type => $"{type.Name}<{string.Join(", ", type.GenericTypeArguments.Select(GetTypeName))}>",
			{ IsArray: true } type => $"{GetTypeName(type.GetElementType()!)}[]",
			{ IsValueType: true } type => type.ToString(),
			{ } type => type.Name
		};
}
