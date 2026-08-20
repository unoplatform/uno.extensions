using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;

[assembly: Uno.Extensions.Reactive.Config.GenerateModelMocks("Given_ModelMocks")]

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_ModelMocks : FeedUITests
{
	#region Fixtures
	public record MockedEntity
	{
		public string? Name { get; init; }
	}

	public partial class MyMockableModel
	{
		// One member of each mapped kind (property- and field-declared), per spec 012 §11.
		public IListFeed<string> AListFeed => ListFeed.Async(async ct => ImmutableList.Create("real") as IImmutableList<string>);

		public IFeed<IImmutableList<string>> AFeedOfList => Feed.Async(async ct => ImmutableList.Create("real") as IImmutableList<string>);

		public IFeed<MockedEntity> AnEntityFeed => Feed.Async(async ct => new MockedEntity { Name = "real" });

		public IFeed<string> AScalarFeed => Feed.Async(async ct => "real");

		public IState<string> AStateFeed => State.Value(this, () => "real");

		public IListFeed<string> AListFeedField = ListFeed.Async(async ct => ImmutableList.Create("real") as IImmutableList<string>);

		public IFeed<IImmutableList<string>> AFeedOfListField = Feed.Async(async ct => ImmutableList.Create("real") as IImmutableList<string>);

		public IFeed<MockedEntity> AnEntityFeedField = Feed.Async(async ct => new MockedEntity { Name = "real" });

		public IFeed<string> AScalarFeedField = Feed.Async(async ct => "real");

		public string APlainProperty { get; set; } = "plain";

		public ValueTask DoSomething(CancellationToken ct)
			=> default;
	}

	public partial class ThrowingModel
	{
		public ThrowingModel()
			=> throw new InvalidOperationException("The model must not be constructed for a mocked view model.");

		public IFeed<string> AFeed => Feed.Async(async ct => "real");
	}

	public partial class MockBaseModel
	{
		public IFeed<string> BaseFeed => Feed.Async(async ct => "base");
	}

	public partial class MockDerivedModel : MockBaseModel
	{
		public IFeed<string> DerivedFeed => Feed.Async(async ct => "derived");
	}
	#endregion

	[TestMethod]
	public async Task When_CreateMock_Then_ReturnsRealViewModelType()
	{
		await using var vm = MyMockableViewModel.CreateMock();

		vm.Should().BeAssignableTo<MyMockableViewModel>();
	}

	[TestMethod]
	public async Task When_CreateMock_Then_ModelIsNotConstructed_And_ModelPropertyIsNull()
	{
		// ThrowingModel's constructor throws: reaching this line proves the model is never constructed.
		await using var vm = ThrowingViewModel.CreateMock();

		vm.Model.Should().BeNull();
	}

	[TestMethod]
	public async Task When_MemberNotMocked_Then_DefaultsToUndefined()
	{
		await using var vm = MyMockableViewModel.CreateMock();

		// The Undefined mock walks through None, so await the state converging to Undefined.
		var data = await vm.AListFeed.DataSet(ct: CT).Where(d => d.Type == OptionType.Undefined).FirstAsync(CT);

		data.Type.Should().Be(OptionType.Undefined);
	}

	[TestMethod]
	public async Task When_ListFeedMocked_Then_ValueFlowsThroughViewModel()
	{
		await using var vm = MyMockableViewModel.CreateMock(m => m.AListFeed = MockListFeed.Value("a", "b"));

		var data = await vm.AListFeed.DataSet(ct: CT).Where(d => d.IsSome(out _)).FirstAsync(CT);

		data.SomeOrDefault()!.Should().BeEquivalentTo(new[] { "a", "b" });
	}

	[TestMethod]
	public async Task When_FeedOfListMocked_Then_ValueFlowsThroughViewModel()
	{
		await using var vm = MyMockableViewModel.CreateMock(m => m.AFeedOfList = MockListFeed.Value("c"));

		var data = await vm.AFeedOfList.DataSet(ct: CT).Where(d => d.IsSome(out _)).FirstAsync(CT);

		data.SomeOrDefault()!.Should().BeEquivalentTo(new[] { "c" });
	}

	[TestMethod]
	public async Task When_ScalarFeedMocked_Then_ValueFlowsThroughViewModel()
	{
		// Created on the dispatcher, like a real page's VM — the bindable sync loop needs it.
		await using var vm = await ExecuteOnDispatcher(() => MyMockableViewModel.CreateMock(m => m.AScalarFeed = MockFeed.Value("42")));

		await WaitFor(() => vm.AScalarFeed, "42");
	}

	[TestMethod]
	public async Task When_StateMocked_Then_ValueFlowsThroughViewModel()
	{
		await using var vm = await ExecuteOnDispatcher(() => MyMockableViewModel.CreateMock(m => m.AStateFeed = MockFeed.Value("pinned")));

		await WaitFor(() => vm.AStateFeed, "pinned");
	}

	[TestMethod]
	public async Task When_EntityFeedMocked_Then_ValueFlowsThroughViewModel()
	{
		await using var vm = await ExecuteOnDispatcher(() => MyMockableViewModel.CreateMock(m => m.AnEntityFeed = MockFeed.Value(new MockedEntity { Name = "mocked" })));

		await WaitFor(() => vm.AnEntityFeed.GetValue()?.Name, "mocked");
	}

	[TestMethod]
	public async Task When_FieldDeclaredFeedsMocked_Then_ValuesFlowThroughViewModel()
	{
		await using var vm = await ExecuteOnDispatcher(() => MyMockableViewModel.CreateMock(m =>
		{
			m.AListFeedField = MockListFeed.Value("lf");
			m.AScalarFeedField = MockFeed.Value("sf");
		}));

		var listData = await vm.AListFeedField.DataSet(ct: CT).Where(d => d.IsSome(out _)).FirstAsync(CT);
		listData.SomeOrDefault()!.Should().BeEquivalentTo(new[] { "lf" });

		await WaitFor(() => vm.AScalarFeedField, "sf");
	}

	[TestMethod]
	public async Task When_CommandMocked_Then_UsesProvidedInstance()
	{
		await using var vm = MyMockableViewModel.CreateMock(m => m.DoSomething = MockCommand.Disabled());

		vm.DoSomething.CanExecute(null).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_CommandNotMocked_Then_IdleMock_And_RealAsyncCommandNotConstructed()
	{
		await using var vm = MyMockableViewModel.CreateMock();

		vm.DoSomething.Should().NotBeOfType<AsyncCommand>();
		vm.DoSomething.CanExecute(null).Should().BeTrue();
		vm.DoSomething.IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_ModelInheritance_Then_MocksBundleChains_And_ValuesFlow()
	{
		typeof(MockDerivedViewModelMocks).Should().BeAssignableTo<MockBaseViewModelMocks>();

		await using var vm = await ExecuteOnDispatcher(() => MockDerivedViewModel.CreateMock(m =>
		{
			m.BaseFeed = MockFeed.Value("b");
			m.DerivedFeed = MockFeed.Value("d");
		}));

		vm.Should().BeAssignableTo<MockBaseViewModel>();
		vm.Should().BeAssignableTo<MockDerivedViewModel>();

		await WaitFor(() => vm.BaseFeed, "b");
		await WaitFor(() => vm.DerivedFeed, "d");
	}

	[TestMethod]
	public async Task When_PlainMemberOnMockedVm_Then_Throws()
	{
		// Non-feed members forward to the Model, which is null on a mocked instance — a documented
		// limitation (cf. doc/Learn/Mvux/Testing.md). This test pins the behavior as a decision.
		await using var vm = MyMockableViewModel.CreateMock();

		vm.Invoking(v => _ = v.APlainProperty).Should().Throw<NullReferenceException>();
	}

	[TestMethod]
	public async Task When_MockedVmDisposed_Then_SourceContextDisposed()
	{
		var vm = MyMockableViewModel.CreateMock();
		var ctx = SourceContext.Find(vm);
		ctx.Should().NotBeNull();

		await vm.DisposeAsync();

		ctx!.Token.IsCancellationRequested.Should().BeTrue();
	}

	[TestMethod]
	public void When_ModelNotMatchingPatterns_Then_NoMockCodeGenerated()
	{
		// 'TestModel' does not match the 'Given_ModelMocks' pattern of the assembly-level GenerateModelMocks
		// attribute, so its generated view model must be identical to a mock-disabled one (spec 012 G5).
		typeof(TestViewModel).GetMethod("CreateMock").Should().BeNull();
		typeof(TestViewModel).Assembly.GetTypes().Should().NotContain(type => type.Name == "TestViewModelMocks");

		// Sweep: no generated view model outside this fixture gained mock members, and no mocks
		// bundle exists for a model outside this fixture.
		var assemblyTypes = typeof(TestViewModel).Assembly.GetTypes();
		assemblyTypes
			.Where(type => type.GetMethod("CreateMock", BindingFlags.Public | BindingFlags.Static) is not null)
			.Should().OnlyContain(type => type.DeclaringType == typeof(Given_ModelMocks));
		assemblyTypes
			.Where(type => type.Name.EndsWith("ViewModelMocks", StringComparison.Ordinal))
			.Should().OnlyContain(type => type.DeclaringType == typeof(Given_ModelMocks));
	}

	[TestMethod]
	public async Task When_MockedTwice_Then_InstancesAreIndependent()
	{
		await using var disabled = MyMockableViewModel.CreateMock(m => m.DoSomething = MockCommand.Disabled());
		await using var idle = MyMockableViewModel.CreateMock();

		disabled.DoSomething.CanExecute(null).Should().BeFalse();
		idle.DoSomething.CanExecute(null).Should().BeTrue();
	}

	// Bounded wait for a bindable property to reflect its pinned value (same pattern as Given_HotReload).
	private static async Task WaitFor<T>(Func<T?> actual, T? expected)
		where T : class
	{
		const int attempts = 100;
		for (var i = 0; i < attempts; i++)
		{
			try
			{
				if (Equals(actual(), expected))
				{
					return;
				}
			}
			catch { }

			await Task.Delay(3);
		}

		throw new TimeoutException($"Expected '{expected}' but got '{actual()}' after {attempts} attempts.");
	}
}
