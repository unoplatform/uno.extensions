using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests.MockingApp.SingleProject;

namespace Uno.Extensions.Reactive.Tests.Mocking;

/// <summary>
/// Spec 013 — the single-project shape: the model and the mocking generator sit in one compilation, so
/// the MVUX metadata attributes are emitted by a sibling generator and cannot be read back. The mock
/// below exists only because the generator analysed the source instead, and its inputs are required only
/// because the service reaching them through a positional record's synthesized property is recognized as
/// a service dependency.
///
/// The types come from the fixture assembly, where they were generated; this project references it and
/// must not generate a second copy of them.
/// </summary>
[TestClass]
public class Given_SingleProjectMock : FeedUITests
{
	private static async Task<IImmutableList<T>> CurrentItems<T>(SourceContext ctx, IListFeed<T> feed)
	{
		var (result, _) = ctx.GetOrCreateListState(feed).Record();

		// Awaits the recorder rather than polling: the deadline is the test library's, and a slow agent
		// fails on the deadline instead of on an assertion that blames the mock.
		await result.WaitForMessages(1);

		result.Last().Current.Data.IsSome(out var value).Should().BeTrue("the mocked input should have been observed");

		return (IImmutableList<T>)value!;
	}

	[TestMethod]
	public void When_ModelInSameCompilation_Then_MockIsGenerated()
	{
		var mock = typeof(PantryModelMock);

		// Required: the service reaching the feed through the record's synthesized property was classified
		// as a service dependency rather than as an independent feed.
		IsRequired(mock.GetProperty(nameof(PantryModel.Items))).Should().BeTrue();

		// The derived feed stays an optional override, and the independent feed is not part of the mock.
		IsRequired(mock.GetProperty(nameof(PantryModel.ItemsCount))).Should().BeFalse();
		mock.GetProperty(nameof(PantryModel.Title)).Should().BeNull();
	}

	[TestMethod]
	public void When_InputIsState_Then_MockExposesItAsItsFeedInterface()
	{
		// A state is an IFeed, but the mock vocabulary hands back an IFeed, so a member typed as the
		// state itself could not accept FeedMock.Empty (CS0266). The mock is typed by the feed interface.
		var filter = typeof(PantryModelMock).GetProperty(nameof(PantryModel.Filter));

		IsRequired(filter).Should().BeTrue();
		filter!.PropertyType.Should().Be(typeof(IFeed<string>));
	}

	[TestMethod]
	public void When_MockGeneratedInReferencedAssembly_Then_NotGeneratedAgainHere()
	{
		// A second copy would carry the same full name, so it would only surface as an ambiguity at the
		// use site: assert that this assembly declares no such type at all.
		typeof(Given_SingleProjectMock).Assembly
			.GetTypes()
			.Should().NotContain(type => type.Name == nameof(PantryModelMock));
	}

	[TestMethod]
	public async Task When_CreateWithMock_Then_FeedEmitsMockedValues()
	{
		var vm = PantryViewModelMock.Create(new PantryModelMock
		{
			Items = global::Uno.HotTesting.Reactive.ListFeedMock.Value("flour", "sugar"),
			Filter = global::Uno.HotTesting.Reactive.FeedMock.Value("dry"),
		});
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Items);
		items.Should().BeEquivalentTo(new[] { "flour", "sugar" });
	}

	private static bool IsRequired(PropertyInfo? property)
	{
		property.Should().NotBeNull();
		return property!.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
	}
}
