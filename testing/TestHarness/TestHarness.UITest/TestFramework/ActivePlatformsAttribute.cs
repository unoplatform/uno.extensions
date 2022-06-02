namespace TestHarness.UITest.TestFramework;

/// <summary>
/// Defines a list of platforms for which the test will be executed. Other platforms will mark the test as ignored.
/// WARNING:
/// This is supported only on UI tests, not for runtime tests.
/// It's available for runtime tests only to ease port of UI tests to runtime test.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
internal class ActivePlatformsAttribute : PropertyAttribute
{
	public Platform[] Platforms
	{
		get
		{
			var property = Properties["ActivePlatforms"] as IList<object>;
			return property?.FirstOrDefault() as Platform[] ?? Array.Empty<Platform>();
		}
	}

	public ActivePlatformsAttribute(params Platform[] platforms)
	{
		Properties.Add("ActivePlatforms", platforms);
	}
}
