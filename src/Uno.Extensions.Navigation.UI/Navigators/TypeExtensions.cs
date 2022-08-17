using System.Reflection;

namespace Uno.Extensions.Navigation.Navigators;

public static class TypeExtensions
{
	public static ConstructorInfo? GetNavigationConstructor(this Type type, INavigator navigator, IServiceProvider services, out object[] constructorArguments)
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
}
