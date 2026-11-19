using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Hosting.Internal;

[TestClass]
public class Given_QueryStringConfiguration
{
	[TestMethod]
	public void When_EveryParameterHasAValue_Then_AllAreRead()
	{
		var values = QueryStringConfiguration.Parse("?a=1&b=two");

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["a"] = "1", ["b"] = "two" });
	}

	[TestMethod]
	public void When_ParameterRepeats_Then_ValuesAreJoined()
	{
		var values = QueryStringConfiguration.Parse("?a=1&a=2");

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["a"] = "1,2" });
	}

	[TestMethod]
	public void When_QueryIsEmpty_Then_NoValues()
	{
		QueryStringConfiguration.Parse(string.Empty).Should().BeEmpty();
	}

	[TestMethod]
	[DataRow("?a=1&flag")]
	[DataRow("?flag&a=1")]
	[DataRow("?a=1&flag=")]
	public void When_ParameterHasNoValue_Then_ItIsReadAsEmpty(string query)
	{
		var values = QueryStringConfiguration.Parse(query);

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["a"] = "1", ["flag"] = string.Empty });
	}

	[TestMethod]
	public void When_SeveralParametersHaveNoValue_Then_EachIsRead()
	{
		var values = QueryStringConfiguration.Parse("?first&second");

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["first"] = string.Empty, ["second"] = string.Empty });
	}

	[TestMethod]
	[DataRow("?a=1&")]
	[DataRow("?a=1&&")]
	[DataRow("?&a=1")]
	public void When_SegmentIsEmpty_Then_ItIsSkipped(string query)
	{
		var values = QueryStringConfiguration.Parse(query);

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["a"] = "1" });
	}

	[TestMethod]
	public void When_EmptySegmentSitsBetweenParameters_Then_BothAreRead()
	{
		var values = QueryStringConfiguration.Parse("?a=1&&b=2");

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["a"] = "1", ["b"] = "2" });
	}

	[TestMethod]
	public void When_ParameterWithoutValueIsEncoded_Then_ItsNameIsDecoded()
	{
		var values = QueryStringConfiguration.Parse("?my%20flag");

		values.Should().BeEquivalentTo(new Dictionary<string, string?> { ["my flag"] = string.Empty });
	}

	[TestMethod]
	[DataRow("?flag&flag=x")]
	[DataRow("?flag=x&FLAG")]
	public void When_ParameterAlsoHasAValue_Then_TheValueWins(string query)
	{
		var values = QueryStringConfiguration.Parse(query);

		values.Should().ContainSingle();
		values.Values.Should().Equal("x");
	}

	[TestMethod]
	public void When_DeepLinkEndsWithParameterWithoutValue_Then_ConfigurationBuilds()
	{
		// Repro of unoplatform/uno.extensions#3291: a bare "autoSubmit" stopped the host from building.
		var query = new Uri("https://localhost:5000/?prompt=Build%20a%20tiny%20app&autoSubmit").Query;

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(QueryStringConfiguration.Parse(query))
			.Build();

		configuration["prompt"].Should().Be("Build a tiny app");
		configuration["autoSubmit"].Should().BeEmpty();
	}
}
