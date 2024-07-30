namespace TestHarness;

[AttributeUsageAttribute(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
public class TestSectionRootAttribute : Attribute
{
	public TestSectionRootAttribute(string name,TestSections section, Type hostInitializer)
	{
		Name = name;
		Section = section;
		HostInitializer = hostInitializer;
	}
	public string Name { get; init; }

	public Type HostInitializer { get; init; }

	public TestSections Section { get; init; }
}
