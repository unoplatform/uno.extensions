using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Sources;

[TestClass]
public class Given_MockFeed : FeedTests
{
	private record MyValue(string Name);

	private record MyEnvelope
	{
		public object? Data { get; init; }
		public bool Progress { get; init; }
		public object? Error { get; init; }
	}

	private record NotAnEnvelope
	{
		public object? Data { get; init; }
		public string? Title { get; init; }
	}

	#region Coercion
	[TestMethod]
	public void When_SourceIsFeed_Then_Passthrough()
	{
		var feed = Feed.Value(42);

		var result = MockFeed.Create(feed);

		result.Should().BeSameAs(feed);
	}

	[TestMethod]
	public void When_EnvelopeDataIsFeed_Then_Passthrough()
	{
		var feed = Feed.Value(42);

		var result = MockFeed.Create(new { Data = feed });

		result.Should().BeSameAs(feed);
	}

	[TestMethod]
	public void When_SameSource_Then_SameFeedInstance()
	{
		var source = new MyValue("42");

		var first = MockFeed.Create(source);
		var second = MockFeed.Create(source);

		second.Should().BeSameAs(first, "coercing the same object twice must preserve feed identity");
	}
	#endregion

	#region Raw values (non-envelope)
	[TestMethod]
	public async Task When_RawPoco_Then_SomeValueAndCompletes()
	{
		var source = new MyValue("42");
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Count.Should().Be(1);
		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(source);
		result.First().Current.Error.Should().BeNull();
		result.First().Current.IsTransient.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_RawString_Then_SomeValue()
	{
		var sut = (IFeed<object>)MockFeed.Create("hello");
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().Be("hello");
	}

	[TestMethod]
	public async Task When_RawList_Then_SomeValue()
	{
		var source = new List<int> { 1, 2, 3 };
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(source);
	}

	[TestMethod]
	public async Task When_PocoWithDataAndOtherProperties_Then_TreatedAsRawValue()
	{
		var source = new NotAnEnvelope { Data = new MyValue("42"), Title = "report" };
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(source, "an object with unrecognized members is not an envelope");
	}
	#endregion

	#region Envelopes (typed)
	[TestMethod]
	public async Task When_EnvelopeWithData_Then_SomeValue()
	{
		var data = new MyValue("42");
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Data = data });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(data, "the envelope's Data must be unwrapped as the feed value");
		result.First().Current.Error.Should().BeNull();
		result.First().Current.IsTransient.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_EnvelopeWithNullData_Then_None()
	{
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Data = null });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.Type.Should().Be(OptionType.None);
	}

	[TestMethod]
	public async Task When_EnvelopeWithProgress_Then_Transient()
	{
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Progress = true });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();
		result.First().Current.Data.Type.Should().Be(OptionType.None, "the typed envelope exposes a Data member whose value is null");
	}

	[TestMethod]
	public async Task When_EnvelopeWithoutDataMember_Then_Undefined()
	{
		var sut = (IFeed<object>)MockFeed.Create(new { Progress = true });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();
		result.First().Current.Data.Type.Should().Be(OptionType.Undefined, "an envelope without a Data member must not force a data state");
	}

	[TestMethod]
	public async Task When_EnvelopeWithProgressAndData_Then_TransientWithValue()
	{
		var data = new MyValue("42");
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Data = data, Progress = true });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();
		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(data);
	}

	[TestMethod]
	public async Task When_EnvelopeWithError_Then_ErrorAxisSet()
	{
		var error = new TestException();
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Error = error });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Error.Should().BeSameAs(error);
	}

	[TestMethod]
	public async Task When_EnvelopeWithErrorAndData_Then_ErrorWithValue()
	{
		var data = new MyValue("42");
		var error = new TestException();
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Data = data, Error = error });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Error.Should().BeSameAs(error);
		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(data);
	}

	[TestMethod]
	public async Task When_AnonymousEnvelopeWithAliases_Then_Recognized()
	{
		var sut = (IFeed<object>)MockFeed.Create(new { IsProgress = true });
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();

		sut = (IFeed<object>)MockFeed.Create(new { InProgress = true });
		result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();

		var error = new TestException();
		sut = (IFeed<object>)MockFeed.Create(new { Exception = error });
		result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Error.Should().BeSameAs(error);
	}
	#endregion

	#region Envelopes (dictionary / expando, i.e. JSON-shaped)
	[TestMethod]
	public async Task When_DictionaryEnvelopeWithCamelCaseKeys_Then_Recognized()
	{
		var data = new MyValue("42");
		var source = new Dictionary<string, object?>
		{
			["data"] = data,
			["progress"] = false,
		};
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(data);
		result.First().Current.IsTransient.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_DictionaryWithUnrecognizedKey_Then_TreatedAsRawValue()
	{
		var source = new Dictionary<string, object?>
		{
			["data"] = new MyValue("42"),
			["title"] = "report",
		};
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(source);
	}

	[TestMethod]
	public async Task When_ExpandoEnvelope_Then_Recognized()
	{
		var source = new ExpandoObject();
		var members = (IDictionary<string, object?>)source;
		members["data"] = new MyValue("42");
		members["error"] = "something went wrong";

		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Error.Should().NotBeNull();
		result.First().Current.Error!.Message.Should().Be("something went wrong", "a non-Exception error member must be wrapped as an exception carrying its text");
	}

	[TestMethod]
	public async Task When_StringProgress_Then_Parsed()
	{
		var source = new Dictionary<string, object?> { ["progress"] = "true" };
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.IsTransient.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_EmptyDictionary_Then_TreatedAsRawValue()
	{
		var source = new Dictionary<string, object?>();
		var sut = (IFeed<object>)MockFeed.Create(source);
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.First().Current.Data.SomeOrDefault().Should().BeSameAs(source, "an object without any envelope member is not an envelope");
	}
	#endregion

	#region Refresh
	[TestMethod]
	public async Task When_RefreshRequested_Then_ReEmitsWithRefreshToken()
	{
		var data = new MyValue("42");
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Data = data });
		var requests = new RequestSource();
		await using var ctx = Context.SourceContext.CreateChild(requests);
		using var result = sut.Record(ctx);

		await result.WaitForMessages(1, CT);

		requests.RequestRefresh();

		await result.WaitForMessages(2, CT);

		result.Count.Should().Be(2);
		result.Last().Current.Data.SomeOrDefault().Should().BeSameAs(data, "refresh must re-emit the mocked value");
		result.Last().Current.Get(MessageAxis.Refresh).Should().NotBeNull("the refresh message must carry the requested token");
	}

	[TestMethod]
	public async Task When_RefreshRequestedOnProgressMock_Then_StaysTransient()
	{
		var sut = (IFeed<object>)MockFeed.Create(new MyEnvelope { Progress = true });
		var requests = new RequestSource();
		await using var ctx = Context.SourceContext.CreateChild(requests);
		using var result = sut.Record(ctx);

		await result.WaitForMessages(1, CT);

		requests.RequestRefresh();

		await result.WaitForMessages(2, CT);

		result.Last().Current.IsTransient.Should().BeTrue("a progress mock models a load that never completes");
	}
	#endregion
}
