﻿namespace TestHarness.UITest.TestFramework;

[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited = true)]
public partial class TestAppModeAttribute : Attribute
{
	/// <summary>
	/// Builds TestMode attribute
	/// </summary>
	/// <param name="cleanEnvironment">
	/// Determines if the app should be restarted to get a clean environment before the fixture tests are started.
	/// </param>
	/// <param name="platform">Determines the target platform to be used for this attribute</param>
	public TestAppModeAttribute(bool cleanEnvironment, Platform platform)
	{
		CleanEnvironment = cleanEnvironment;
		Platform = platform;
	}

	/// <summary>
	/// Determines if the app should be restarted to get a clean environment before the fixture tests are started.
	/// </summary>
	public bool CleanEnvironment { get; }

	/// <summary>
	/// Determines the target platform to be used for this attribute
	/// </summary>
	public Platform Platform { get; }
}
