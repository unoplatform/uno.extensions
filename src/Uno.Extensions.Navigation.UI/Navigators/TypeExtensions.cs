using System.Reflection;

namespace Uno.Extensions.Navigation.Navigators;

public static class TypeExtensions
{
	public static ConstructorInfo? GetNavigationConstructor(this Type type, INavigator navigator, IServiceProvider services, out object[] constructorArguments)
	{
		var ctrs = type.GetConstructors();
		foreach (var ctr in ctrs)
		{
			var paras = ctr.GetParameters();
			var args = new List<object>();
			var isValid = true;
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
					isValid = false;
					break;
				}
			}
			if (isValid)
			{
				constructorArguments = args.ToArray();
				return ctr;
			}
		}

		constructorArguments = new object[] { };
		return null;
	}
}
