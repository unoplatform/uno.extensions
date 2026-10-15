using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Uno.Extensions.Navigation;

public static class AssemblyExtensions
{
	public static IList<string> Excludes { get; } = new List<string>();

	[RequiresUnreferencedCode("From Assembly.GetTypes(): Types might be removed")]
	public static Type[] SafeGetTypes(this Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch
		{
			if (assembly.FullName is not null)
			{
				Excludes.Add(assembly.FullName);
			}
			return new Type[] { };
		}
	}
}
