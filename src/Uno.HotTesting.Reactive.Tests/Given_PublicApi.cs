using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;

namespace Uno.HotTesting.Reactive.Tests;

[TestClass]
public class Given_PublicApi
{
	[TestMethod]
	public void When_AssemblyIsLoaded_Then_NameAndNamespaceMatchProductContract()
	{
		typeof(FeedMock).Assembly.GetName().Name.Should().Be("Uno.HotTesting.Reactive");
		typeof(FeedMock).Namespace.Should().Be("Uno.HotTesting.Reactive");
		typeof(ListFeedMock).Namespace.Should().Be("Uno.HotTesting.Reactive");
	}

	[TestMethod]
	public void When_FactorySurfaceIsInspected_Then_OnlyPinnedStatePrimitivesArePresent()
	{
		var expected = new[]
		{
			"Empty",
			"Error",
			"Loading",
			"Message",
			"Refreshing",
			"Undefined",
			"Value",
		};

		typeof(FeedMock).GetMethods()
			.Where(method => method.DeclaringType == typeof(FeedMock))
			.Select(method => method.Name)
			.Distinct()
			.Should()
			.BeEquivalentTo(expected);
		typeof(ListFeedMock).GetMethods()
			.Where(method => method.DeclaringType == typeof(ListFeedMock))
			.Select(method => method.Name)
			.Distinct()
			.Should()
			.BeEquivalentTo(expected);
	}

	[TestMethod]
	public void When_UserBuildsNamedRecord_Then_FeedsCanBeInjectedDirectly()
		=> new PreviewModel(FeedMock.Empty<int>(), ListFeedMock.Empty<string>())
			.Should().BeAssignableTo<PreviewModel>();

	private sealed record PreviewModel(IFeed<int> Count, IListFeed<string> Items);
}
