using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uno.Extensions.Navigation.Navigators;

public static class TypeExtensions
{
	public static ConstructorInfo? GetNavigationConstructor(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		this Type type,
		INavigator navigator,
		IServiceProvider services,
		out object[] constructorArguments)
	{
		var ctr = type.GetConstructors().FirstOrDefault();
		if (ctr is not null)
		{
			var paras = ctr.GetParameters();
			var args = new List<object>();
			foreach (var para in paras)
			{
				if (para.ParameterType == typeof(IServiceProvider))
				{
					args.Add(services);
				}
				else if (para.ParameterType == typeof(INavigator))
				{
					args.Add(navigator);
				}
				else
				{
					var arg = services.GetService(para.ParameterType);
					args.Add(arg!);
				}
			}
			constructorArguments = args.ToArray();
			return ctr;
		}

		constructorArguments = new object[] { };
		return null;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "if the constructor is removed, CreateInstance throws, which is transformed into null, handled at callsites.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "if it throws, return null; null is already handled at callsites")]
	internal static T? CreateInstance<T>(this Type? type, ILogger? logger)
		where T : class
	{
		if (type == null)
		{
			return default;
		}
		try
		{
			return Activator.CreateInstance(type) as T;
		}
		catch(Exception e)
		{
			if (logger?.IsEnabled(LogLevel.Warning) ?? false)
			{
				logger.LogWarning(e, $"Exception when calling `Activator.CreateInstance(typeof({type.FullName}))`: {e.Message}");
			}
			return default;
		}
	}
}
