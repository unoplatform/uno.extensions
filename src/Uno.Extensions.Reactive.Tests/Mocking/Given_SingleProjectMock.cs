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
/// below exists only because the generator analysed the source instead, and its <c>Items</c> member is
/// required only because the service reaching the feed through a positional record's synthesized
/// property is recognized as a service dependency.
///
/// The types come from the fixture assembly, where they were generated; this project references it and
/// must NOT generate a second copy of them (same name, same namespace, two assemblies).
/// </summary>
[TestClass]
public class Given_SingleProjectMock : FeedUITests
{
	private static async Task<IImmutableList<T>?> CurrentItems<T>(SourceContext ctx, IListFeed<T> feed)
	{
		var (result, _) = ctx.GetOrCreateListState(feed).Record();
		for (var i = 0; i < 50; i++)
		{
			if (result.Count > 0 && result.Last().Current.Data.IsSome(out var v))
			{
				return (IImmutableList<T>)v!;
			}
			await Task.Delay(20);
		}
		return result.Count > 0 && result.Last().Current.Data.IsSome(out var last) ? (IImmutableList<T>)last! : null;
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

	private static bool IsRequired(PropertyInfo? property)
	{
		property.Should().NotBeNull();
		return property!.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
	}

	[TestMethod]
	public void When_MockGeneratedInReferencedAssembly_Then_NotGeneratedAgainHere()
	{
		// Both copies would carry the same full name, so the duplicate would only surface as an ambiguity
		// at the use site: assert on the declaring assembly instead.
		typeof(PantryModelMock).Assembly
			.Should().NotBeSameAs(typeof(Given_SingleProjectMock).Assembly);
	}

	[TestMethod]
	public async Task When_CreateWithMock_Then_FeedEmitsMockedValues()
	{
		var vm = PantryViewModelMock.Create(new PantryModelMock { Items = global::Uno.HotTesting.Reactive.ListFeedMock.Value("flour", "sugar") });
		using var _ = SourceContext.GetOrCreate(vm.Model).AsCurrent();

		var items = await CurrentItems(SourceContext.GetOrCreate(vm.Model), vm.Model.Items);
		items.Should().BeEquivalentTo(new[] { "flour", "sugar" });
	}

}
