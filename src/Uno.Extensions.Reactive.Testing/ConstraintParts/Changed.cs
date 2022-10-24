using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;

namespace Uno.Extensions.Reactive.Testing;

public class Changed : ChangesConstraint
{
	public static Changed None { get; } = new(Array.Empty<string>());

	public static Changed Data { get; } = new(MessageAxis.Data);

	public static Changed Error { get; } = new(MessageAxis.Error);

	public static Changed Progress { get; } = new(MessageAxis.Progress);

	public static Changed Refreshed { get; } = new(MessageAxis.Refresh);

	public static Changed Pagination { get; } = new(MessageAxis.Pagination);

	public static Changed Selection { get; } = new(MessageAxis.Selection);

	public static Changed Axes(params string[] axisIdentifiers)
		=> new(axisIdentifiers);

	public static Changed Axes(params MessageAxis[] axes) 
		=> new(axes);

	public readonly string[] Expected;

	public Changed(params MessageAxis[] expected)
		=> Expected = expected.Select(a => a.Identifier).ToArray();

	public Changed(params string[] expectedIdentifiers)
		=> Expected = expectedIdentifiers;

	/// <inheritdoc />
	public override void Assert(ChangeCollection changes)
		=> changes.Select(a => a.Identifier).Should().BeEquivalentTo(Expected);

	public static Changed operator &(Changed left, Changed right)
		=> new(left.Expected.Concat(right.Expected).ToArray());

	public static Changed operator &(Changed left, MessageAxis axis)
		=> new(left.Expected.Concat(new[] { axis.Identifier }).ToArray());

	public static Changed operator &(Changed left, string axisIdentifier)
		=> new(left.Expected.Concat(new[] { axisIdentifier }).ToArray());
}
