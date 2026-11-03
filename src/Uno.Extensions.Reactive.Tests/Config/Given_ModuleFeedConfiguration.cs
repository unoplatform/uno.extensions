using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Config;

namespace Uno.Extensions.Reactive.Tests.Config;

[TestClass]
public class Given_ModuleFeedConfiguration
{
	[TestMethod]
	public void When_EntryAssemblyKnown_Then_OnlyMatchingModuleIsApp()
	{
		var isConfigured = 0;

		ModuleFeedConfiguration.IsAppModule("MyLib", "MyApp", ref isConfigured).Should().BeFalse();
		ModuleFeedConfiguration.IsAppModule("myapp", "MyApp", ref isConfigured).Should().BeTrue();
	}

	[TestMethod]
	public void When_NoEntryAssembly_Then_FirstModuleIsApp()
	{
		// Android (MonoVM and CoreCLR): Assembly.GetEntryAssembly() is null, which used to leave MVUX hot-reload disabled (unoplatform/uno#25417).
		var isConfigured = 0;

		ModuleFeedConfiguration.IsAppModule("MyApp", null, ref isConfigured).Should().BeTrue();
		ModuleFeedConfiguration.IsAppModule("MyLib", null, ref isConfigured).Should().BeFalse();
	}
}
