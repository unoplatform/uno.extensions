using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_TokenSet
{
	[TestMethod]
	public void When_Aggregate_Then_KeepOnlyLatestToken()
	{
		var t1 = Get(0, 0);
		var t2 = Get(0, 1);
		var t3 = Get(0, 2);

		var result = TokenSet<TestToken>.Aggregate(new [] { t1, t2, t3 }).Tokens;

		result.Count.Should().Be(1);
		result.Single().RootContextId.Should().Be(0);
		result.Single().SequenceId.Should().Be(2);
	}

	[TestMethod]
	public void When_Aggregate_Then_DistinctOnContextId()
	{
		var t0_1 = Get(0, 0);
		var t0_2 = Get(0, 1);
		var t1_1 = Get(1, 2);
		var t1_2 = Get(1, 3);

		var result = TokenSet<TestToken>.Aggregate(new[] { t0_1, t0_2, t1_1, t1_2 }).Tokens;

		result.Count.Should().Be(2);
		result.Single(t => t.RootContextId == 0).SequenceId.Should().Be(1);
		result.Single(t => t.RootContextId == 1).SequenceId.Should().Be(3);
	}

	[TestMethod]
	public void When_Aggregate_Then_DistinctOnSource()
	{
		var src0 = new object();
		var src1 = new object();
		var t0_1 = Get(src0, 0, 0);
		var t0_2 = Get(src0, 0, 1);
		var t1_1 = Get(src1, 0, 2);
		var t1_2 = Get(src1, 0, 3);

		var result = TokenSet<TestToken>.Aggregate(new[] { t0_1, t0_2, t1_1, t1_2 }).Tokens;

		result.Count.Should().Be(2);
		result.Single(t => t.Source == src0).SequenceId.Should().Be(1);
		result.Single(t => t.Source == src1).SequenceId.Should().Be(3);
	}

	[TestMethod]
	public void When_Aggregate_Then_KeepOnlyLatestTokenPerSourceAndContext()
	{
		var src0 = new object();
		var src1 = new object();
		var t0_0_0 = Get(src0, 0, 0);
		var t0_0_1 = Get(src0, 0, 1);
		var t0_1_2 = Get(src0, 1, 2);
		var t0_1_3 = Get(src0, 1, 3);
		var t1_0_4 = Get(src1, 0, 4);
		var t1_0_5 = Get(src1, 0, 5);
		var t1_2_6 = Get(src1, 2, 6);
		var t1_2_7 = Get(src1, 2, 7);

		var result = TokenSet<TestToken>.Aggregate(new[] { t0_0_0, t0_0_1, t0_1_2, t0_1_3, t1_0_4, t1_0_5, t1_2_6, t1_2_7 }).Tokens;

		result.Count.Should().Be(4);

		result.GroupBy(t => t.Source).Single(g => g.Key == src0).Single(t => t.RootContextId is 0).SequenceId.Should().Be(1);
		result.GroupBy(t => t.Source).Single(g => g.Key == src0).Single(t => t.RootContextId is 1).SequenceId.Should().Be(3);
		result.GroupBy(t => t.Source).Single(g => g.Key == src1).Single(t => t.RootContextId is 0).SequenceId.Should().Be(5);
		result.GroupBy(t => t.Source).Single(g => g.Key == src1).Single(t => t.RootContextId is 2).SequenceId.Should().Be(7);
	}

	[TestMethod]
	public void When_IsGreaterOrEqualsThan()
	{
		var sut = Get((0, 42), (1, 42));

		sut.IsGreaterOrEquals(Get(0, 41)).Should().BeFalse(because: "no token for ctx 1 and lower than 42");
		sut.IsGreaterOrEquals(Get(0, 42)).Should().BeFalse(because: "no token for ctx 1");
		sut.IsGreaterOrEquals(Get(0, 43)).Should().BeFalse(because: "no token for ctx 1");

		sut.IsGreaterOrEquals(Get((0, 41), (1, 41))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 42), (1, 41))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 43), (1, 41))).Should().BeFalse(because: "token for ctx 0 is lower than 43");

		sut.IsGreaterOrEquals(Get((0, 41), (1, 42))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 42), (1, 42))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 43), (1, 42))).Should().BeFalse(because: "token for ctx 0 is lower than 43");

		sut.IsGreaterOrEquals(Get((0, 41), (1, 43))).Should().BeFalse(because: "token for ctx 1 is lower than 44");
		sut.IsGreaterOrEquals(Get((0, 42), (1, 43))).Should().BeFalse(because: "token for ctx 1 is lower than 44");
		sut.IsGreaterOrEquals(Get((0, 43), (1, 43))).Should().BeFalse(because: "token for ctx 0 is lower than 43 and token for ctx 1 is lower than 44");

		// We are ignoring the token for the third context
		sut.IsGreaterOrEquals(Get((0, 41), (1, 41), (2, 1))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 42), (1, 41), (2, 1))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 43), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 43");

		sut.IsGreaterOrEquals(Get((0, 41), (1, 42), (2, 1))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 42), (1, 42), (2, 1))).Should().BeTrue();
		sut.IsGreaterOrEquals(Get((0, 43), (1, 42), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 43");

		sut.IsGreaterOrEquals(Get((0, 41), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 1 is lower than 44");
		sut.IsGreaterOrEquals(Get((0, 42), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 1 is lower than 44");
		sut.IsGreaterOrEquals(Get((0, 43), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 43 and token for ctx 1 is lower than 44");
	}

	[TestMethod]
	public void When_Empty_Then_IsGreaterOrEqualsThan()
	{
		var sut = Get();

		sut.IsEmpty.Should().BeTrue();

		sut.IsGreaterOrEquals(Get((0, 42))).Should().BeFalse();
		sut.IsGreaterOrEquals(Get()).Should().BeTrue();
	}

	[TestMethod]
	public void When_IsLowerThan()
	{
		var sut = Get((0, 42), (1, 42));

		sut.IsLower(Get(0, 41)).Should().BeFalse(because: "token for ctx 0 is lower than 42 and no token for ctx 1");
		sut.IsLower(Get(0, 42)).Should().BeFalse(because: "token for ctx 0 is 42 and no token for ctx 1");
		sut.IsLower(Get(0, 43)).Should().BeFalse(because: "no token for ctx 1");

		sut.IsLower(Get((0, 41), (1, 41))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 1 is lower than 42");
		sut.IsLower(Get((0, 42), (1, 41))).Should().BeFalse(because: "token for ctx 0 is 42 token for ctx 1 is lower than 42");
		sut.IsLower(Get((0, 43), (1, 41))).Should().BeFalse(because: "token for ctx 1 is lower than 42");

		sut.IsLower(Get((0, 41), (1, 42))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 2 is 42");
		sut.IsLower(Get((0, 42), (1, 42))).Should().BeFalse(because: "token for ctx 0 is 42 and token for ctx 1 is 42");
		sut.IsLower(Get((0, 43), (1, 42))).Should().BeFalse(because: "token for ctx 1 is 42");

		sut.IsLower(Get((0, 41), (1, 43))).Should().BeFalse(because: "token for ctx 0 is lower than 42");
		sut.IsLower(Get((0, 42), (1, 43))).Should().BeFalse(because: "token for ctx 0 is 42");
		sut.IsLower(Get((0, 43), (1, 43))).Should().BeTrue();

		// We are ignoring the token for the third context
		sut.IsLower(Get((0, 41), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 1 is lower than 42");
		sut.IsLower(Get((0, 42), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 0 is 42 token for ctx 1 is lower than 42");
		sut.IsLower(Get((0, 43), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 1 is lower than 42");

		sut.IsLower(Get((0, 41), (1, 42), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 2 is 42");
		sut.IsLower(Get((0, 42), (1, 42), (2, 1))).Should().BeFalse(because: "token for ctx 0 is 42 and token for ctx 1 is 42");
		sut.IsLower(Get((0, 43), (1, 42), (2, 1))).Should().BeFalse(because: "token for ctx 1 is 42");

		sut.IsLower(Get((0, 41), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42");
		sut.IsLower(Get((0, 42), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 0 is 42");
		sut.IsLower(Get((0, 43), (1, 43), (2, 1))).Should().BeTrue();
	}

	[TestMethod]
	public void When_Empty_Then_IsLowerThan()
	{
		var sut = Get();

		sut.IsEmpty.Should().BeTrue();

		sut.IsLower(Get((0, 42))).Should().BeTrue();
		sut.IsLower(Get()).Should().BeFalse();
	}

	[TestMethod]
	public void When_IsLowerOrEqualsThan()
	{
		var sut = Get((0, 42), (1, 42));

		sut.IsLowerOrEquals(Get(0, 41)).Should().BeFalse(because: "token for ctx 0 is lower than 42 and no token for ctx 1");
		sut.IsLowerOrEquals(Get(0, 42)).Should().BeFalse(because: "token for ctx 0 is 42 and no token for ctx 1");
		sut.IsLowerOrEquals(Get(0, 43)).Should().BeFalse(because: "no token for ctx 1");

		sut.IsLowerOrEquals(Get((0, 41), (1, 41))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 1 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 41))).Should().BeFalse(because: "token for ctx 0 is 42 token for ctx 1 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 43), (1, 41))).Should().BeFalse(because: "token for ctx 1 is lower than 42");

		sut.IsLowerOrEquals(Get((0, 41), (1, 42))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 2 is 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 42))).Should().BeTrue();
		sut.IsLowerOrEquals(Get((0, 43), (1, 42))).Should().BeTrue();

		sut.IsLowerOrEquals(Get((0, 41), (1, 43))).Should().BeFalse(because: "token for ctx 0 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 43))).Should().BeTrue();
		sut.IsLowerOrEquals(Get((0, 43), (1, 43))).Should().BeTrue();

		// We are ignoring the token for the third context
		sut.IsLowerOrEquals(Get((0, 41), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 1 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 0 is 42 token for ctx 1 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 43), (1, 41), (2, 1))).Should().BeFalse(because: "token for ctx 1 is lower than 42");

		sut.IsLowerOrEquals(Get((0, 41), (1, 42), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42 and token for ctx 2 is 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 42), (2, 1))).Should().BeTrue();
		sut.IsLowerOrEquals(Get((0, 43), (1, 42), (2, 1))).Should().BeTrue();

		sut.IsLowerOrEquals(Get((0, 41), (1, 43), (2, 1))).Should().BeFalse(because: "token for ctx 0 is lower than 42");
		sut.IsLowerOrEquals(Get((0, 42), (1, 43), (2, 1))).Should().BeTrue();
		sut.IsLowerOrEquals(Get((0, 43), (1, 43), (2, 1))).Should().BeTrue();
	}

	[TestMethod]
	public void When_Empty_Then_IsLowerOrEqualsThan()
	{
		var sut = Get();

		sut.IsEmpty.Should().BeTrue();

		sut.IsLowerOrEquals(Get((0,42))).Should().BeTrue();
		sut.IsLowerOrEquals(Get()).Should().BeTrue();
	}

	private TokenSet<TestToken> Get(params (uint ctx, uint seq)[] seqs) => new(seqs.Select(v => new TestToken(this, v.ctx, v.seq)).ToImmutableList());

	private TokenSet<TestToken> Get(uint ctx, uint seq) => new(ImmutableList.Create(new TestToken(this, ctx, seq)));

	private TokenSet<TestToken> Get(object source, uint ctx, uint seq) => new(ImmutableList.Create(new TestToken(source, ctx, seq)));

	private record TestToken(object Source, uint RootContextId, uint SequenceId) : IToken;
}
