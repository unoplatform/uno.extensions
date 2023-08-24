using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Reactive.Utils;

internal static class ActivatorHelper
{
	internal static object CreateInstance(Type type, (Type type, string name, object? value)[] arguments, Func<Type, string, object?>? tryGetMissingArgument = null)
	{
		var ctors = type
			.GetConstructors(BindingFlags.CreateInstance | BindingFlags.Public)
			.Select(GetArguments)
			.OrderByDescending(ctor => ctor.rank);
		foreach (var ctor in ctors)
		{
			try
			{
				return ctor.method.Invoke(ctor.arguments);
			}
			catch { }
		}

		throw new InvalidOperationException($"No compatible constructor found for type {type.FullName}");

		(ConstructorInfo method, int rank, object?[] arguments) GetArguments(ConstructorInfo ctor)
		{
			var parameters = ctor.GetParameters();

			// Parameter-less ctor
			if (parameters is { Length: 0 } && arguments is { Length: 0 })
			{
				return (ctor, int.MaxValue, Array.Empty<object>());
			}

			// All arguments are in the same order
			if (parameters.Length == arguments.Length
				&& parameters.Select((param, i) => param.ParameterType.IsAssignableFrom(arguments[i].type)).All(isCompatible => isCompatible))
			{
				return (ctor, int.MaxValue, arguments.Select(arg => arg.value).ToArray());
			}

			// Do our best to find matching ctor arguments
			var rank = 0;
			var oldArguments = arguments.ToList();
			var newArguments = new List<(bool hasValue, object? value)>(parameters.Length);

			// First pass using name and type of arguments
			for (var i = 0; i < parameters.Length; i++)
			{
				var param = parameters[i];
				if (oldArguments.FirstOrDefault(a => param.Name == a.name && param.ParameterType.IsAssignableFrom(a.type)) is { type: not null } arg)
				{
					rank += 2;
					newArguments[i] = (true, arg.value);
					oldArguments.Remove(arg); // Do not re-use args!
				}
			}

			// Second (and third pass) using type only (or fallback)
			for (var i = 0; i < parameters.Length; i++)
			{
				if (newArguments[i].hasValue)
				{
					continue;
				}

				var param = parameters[i];
				if (oldArguments.FirstOrDefault(a => param.ParameterType.IsAssignableFrom(a.type)) is { type: not null } arg)
				{
					rank++;
					newArguments.Add((true, arg.value));
					oldArguments.Remove(arg); // Do not re-use args!
				}
				else
				{
					// Not found, try for get it from DI, or use null.
					newArguments.Add((false, tryGetMissingArgument?.Invoke(param.ParameterType, param.Name)));
				}
			}

			// If we have remaining arguments, then rank is lower
			rank -= oldArguments.Count * 2;

			return (ctor, rank, newArguments.Select(a => a.value).ToArray());
		}
	}
}
