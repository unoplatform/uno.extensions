using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Testing;

public sealed class Refreshed : AxisConstraint
{
	public static Refreshed Is(uint version)
		=> new(new RefreshConstraint { Version = version });

	public static Refreshed By(object source, uint version)
		=> new(new RefreshConstraint { Source = source, Version = version });

	private readonly RefreshConstraint[] _expected;

	public Refreshed(params RefreshConstraint[] expected)
	{
		_expected = expected;
	}

	/// <inheritdoc />
	public override void Assert(IMessageEntry entry)
	{
		var actual = MessageAxis.Refresh.FromMessageValue(entry[Axis]);
		if (actual is null or {IsEmpty: true})
		{
			if (_expected.Length != 0)
			{
				AssertionScope.Current.FailWith(AssertionScope.Current.Context.Value + " no source version has been defined in the message.");
			}
			return;
		}

		actual.Tokens.Count.Should().Be(_expected.Length, "all source versions should be defined.");

		var expected = _expected.ToList();
		foreach (var actualVersion in actual.Tokens)
		{
			var expectedVersion = expected
				.Select(version => (version, score: version.Match(actualVersion)))
				.Where(x => x.score > 0)
				.OrderByDescending(x => x.score)
				.FirstOrDefault()
				.version;

			if (expectedVersion is null)
			{
				AssertionScope.Current.FailWith($"{AssertionScope.Current.Context.Value} feed {GetDebugIdentifier(actualVersion.Source)} version {actualVersion.SequenceId} for context #{actualVersion.RootContextId} was not expected.");
			}
			else
			{
				expected.Remove(expectedVersion);
			}
		}
	}

	/// <inheritdoc />
	public override MessageAxis Axis => MessageAxis.Refresh;
}
