namespace TestHarness;

public class TestSectionRootAttribute : Attribute
{
	public TestSectionRootAttribute(string name, Type hostInitializer)
	{
		Name = name;
		HostInitializer = hostInitializer;
	}
	public string Name { get; init; }

	public Type HostInitializer { get; init; }
}
