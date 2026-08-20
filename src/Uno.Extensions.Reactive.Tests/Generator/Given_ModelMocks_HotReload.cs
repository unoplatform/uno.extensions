using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_ModelMocks_HotReload : FeedUITests
{
	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		FeedConfiguration.HotReload = HotReloadSupport.Enabled;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		base.Initialize();
	}

	/// <inheritdoc />
	[TestCleanup]
	public override void Cleanup()
	{
		FeedConfiguration.HotReload = null;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		base.Cleanup();
	}

	[TestMethod]
	public async Task When_MockedVm_Then_HotPatchDoesNotReplaceMocks()
	{
		// A regular instance registers itself for hot reload; a mocked one must not (spec 012 §7.6).
		await using var normal = new Given_ModelMocks.MyMockableViewModel();
		await using var mocked = Given_ModelMocks.MyMockableViewModel.CreateMock(m => m.DoSomething = MockCommand.Disabled());

		var normalCommand = normal.DoSomething;
		var mockedCommand = mocked.DoSomething;

		BindableViewModelBase.HotPatch(
			typeof(Given_ModelMocks.MyMockableViewModel),
			typeof(Given_ModelMocks.MyMockableModel),
			typeof(Given_ModelMocks.MyMockableModel));

		normal.DoSomething.Should().NotBeSameAs(normalCommand, "hot patch must re-initialize regular instances (guards this test against passing vacuously)");
		mocked.DoSomething.Should().BeSameAs(mockedCommand, "a mocked instance must not be hot-patched");
		mocked.Model.Should().BeNull();
	}
}
