using System;
using FluentAssertions;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Uno.Extensions.Reactive.SelectionInfo;

namespace Uno.Extensions.Reactive.Tests.Core.Axes;

[TestClass]
public class Given_SelectionInfo
{
	[TestMethod]
	public void When_Contains()
	{
		Empty.Contains(42).Should().BeFalse();

		Single(42).Contains(41).Should().BeFalse();
		Single(42).Contains(42).Should().BeTrue();
		Single(42).Contains(43).Should().BeFalse();

		Multiple(42, 3).Contains(41).Should().BeFalse();
		Multiple(42, 3).Contains(42).Should().BeTrue();
		Multiple(42, 3).Contains(43).Should().BeTrue();
		Multiple(42, 3).Contains(44).Should().BeTrue();
		Multiple(42, 3).Contains(45).Should().BeFalse();
	}

	[TestMethod]
	[DynamicData(nameof(GetAddCases), DynamicDataSourceType.Method)]
	public void When_Add(TestCase @case)
	{
		var result = @case.Original.Add(@case.Range);

		result.ToString().Should().Be(@case.Result);
	}

	private static IEnumerable<object[]> GetAddCases()
	{
		return GetCases()
			//.Where(@case => @case.Line == 49)
			.Select(@case => new object[] { @case with { Op = "+" } });

		IEnumerable<TestCase> GetCases()
		{
			yield return new(Empty, new SelectionIndexRange(42, 0), "--Empty--");
			yield return new(Empty, new SelectionIndexRange(42, 1), "[42, 42]");

			// Overlapping and concomitant ranges
			yield return new(Single(42), new SelectionIndexRange(40, 2), "[40, 42]");

			yield return new(Single(42), new SelectionIndexRange(41, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(41, 1), "[41, 42]");
			yield return new(Single(42), new SelectionIndexRange(41, 2), "[41, 42]");
			yield return new(Single(42), new SelectionIndexRange(41, 3), "[41, 43]");

			yield return new(Single(42), new SelectionIndexRange(42, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(42, 1), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(42, 2), "[42, 43]");

			yield return new(Single(42), new SelectionIndexRange(43, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(43, 1), "[42, 43]");
			yield return new(Single(42), new SelectionIndexRange(43, 2), "[42, 44]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(40, 2), "[40, 44]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 1), "[41, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 4), "[41, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 5), "[41, 45]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 1), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 4), "[42, 45]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 1), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 3), "[42, 45]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 1), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 2), "[42, 45]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(45, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(45, 1), "[42, 45]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(45, 2), "[42, 46]");

			// Disjoint ranges
			yield return new(Single(42), new SelectionIndexRange(5, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(5, 1), "[5, 5] & [42, 42]");
			yield return new(Single(42), new SelectionIndexRange(5, 2), "[5, 6] & [42, 42]");
			yield return new(Single(42), new SelectionIndexRange(40, 1), "[40, 40] & [42, 42]");

			yield return new(Single(42), new SelectionIndexRange(50, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(50, 1), "[42, 42] & [50, 50]");
			yield return new(Single(42), new SelectionIndexRange(50, 2), "[42, 42] & [50, 51]");
		}
	}

	[TestMethod]
	[DynamicData(nameof(GetRemoveCases), DynamicDataSourceType.Method)]
	public void When_Remove(TestCase @case)
	{
		var result = @case.Original.Remove(@case.Range);

		result.ToString().Should().Be(@case.Result);
	}

	private static IEnumerable<object[]> GetRemoveCases()
	{
		return GetCases()
			//.Where(@case => @case.Line == 141)
			.Select(@case => new object[] { @case with { Op = "-" } });

		IEnumerable<TestCase> GetCases()
		{
			yield return new(Empty, new SelectionIndexRange(42, 0), "--Empty--");
			yield return new(Empty, new SelectionIndexRange(42, 1), "--Empty--");

			// Overlapping and concomitant ranges
			yield return new(Single(42), new SelectionIndexRange(40, 2), "[42, 42]");

			yield return new(Single(42), new SelectionIndexRange(41, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(41, 1), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(41, 2), "--Empty--");
			yield return new(Single(42), new SelectionIndexRange(41, 3), "--Empty--");

			yield return new(Single(42), new SelectionIndexRange(42, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(42, 1), "--Empty--");
			yield return new(Single(42), new SelectionIndexRange(42, 2), "--Empty--");

			yield return new(Single(42), new SelectionIndexRange(43, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(43, 1), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(43, 2), "[42, 42]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(40, 2), "[42, 44]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 1), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 2), "[43, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 3), "[44, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 5), "--Empty--");
			yield return new(Multiple(42, 3), new SelectionIndexRange(41, 6), "--Empty--");

			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 1), "[43, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 2), "[44, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 3), "--Empty--");
			yield return new(Multiple(42, 3), new SelectionIndexRange(42, 4), "--Empty--");

			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 1), "[42, 42] & [44, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 2), "[42, 42]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(43, 3), "[42, 42]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 1), "[42, 43]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(44, 2), "[42, 43]");

			yield return new(Multiple(42, 3), new SelectionIndexRange(45, 0), "[42, 44]");
			yield return new(Multiple(42, 3), new SelectionIndexRange(45, 1), "[42, 44]");

			// Disjoint ranges
			yield return new(Single(42), new SelectionIndexRange(5, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(5, 1), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(5, 2), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(40, 1), "[42, 42]");

			yield return new(Single(42), new SelectionIndexRange(50, 0), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(50, 1), "[42, 42]");
			yield return new(Single(42), new SelectionIndexRange(50, 2), "[42, 42]");
		}
	}

	private static SelectionInfo Multiple(uint from, uint length)
		=> Empty.Add(new SelectionIndexRange(from, length));

	public record struct TestCase(SelectionInfo Original, SelectionIndexRange Range, string Result, [CallerLineNumber] int Line = -1)
	{
		public string Op { get; init; } = "with";

		public override string ToString() => $"L{Line}: {Original} {Op} {Range} = {Result}";
	}
}
