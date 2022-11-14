using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests.Generator;

public partial class Given_BasicViewModel_Then_Generate__ViewModel
{
	public Given_BasicViewModel_Then_Generate__ViewModel()
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(string aRandomService)
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(
		IFeed<string> anExternalInput,
		IState<string> anExternalReadWriteInput,
		IFeed<MyRecord> anExternalRecordInput,
		IFeed<MyWeirdRecord> anExternalWeirdRecordInput)
	{
	}

	public string AnInputConflictingWithAProperty { get; } = "AnInputConflictingWithAProperty";

	public string AField = "AField";

	internal string AnInternalField = "AnInternalField";

	protected internal string AProtectedInternalField = "AProtectedInternalField";

	public IFeed<string> AFeedField = Feed.Async(async ct => "42");

	public IState<string> AStateField = new StateImpl<string>(SourceContext.Current, Feed.Async(async ct => "42"));

	public IFeed<MyRecord> ARecordFeedField = Feed.Async(async ct => new MyRecord("ARecordFeedField", 42, default, new MyWeirdRecord()));

	public IState<MyRecord> ARecordStateField = State<MyRecord>.Async(async ct => new MyRecord("ARecordStateField", 42, default, new MyWeirdRecord()));

	public IListFeed<string> AListFeedField = Feed.Async(async ct => new[] { "AListFeedField_1", "AListFeedField_2" }.ToImmutableList()).AsListFeed();

	public IListState<string> AListStateField = new ListStateImpl<string>(new StateImpl<IImmutableList<string>>(SourceContext.Current, Feed.Async(async ct => new[] { "AListStateField_1", "AListStateField_2" }.ToImmutableList() as IImmutableList<string>)));

	public CustomFeed ACustomFeedField = new CustomFeed();

	public string AProperty { get; set; } = "AProperty";

	internal string AnInternalProperty { get; set; } = "AnInternalProperty";

	protected internal string AProtectedInternalProperty { get; set; } = "AProtectedInternalProperty";

	public string AReadOnlyProperty { get; } = nameof(AReadOnlyProperty);

	public string ASetOnlyProperty { set { } }

	public IFeed<string> AFeedProperty { get; } = Feed.Async(async ct => "AFeedProperty");

	public IState<string> AStateProperty { get; } = new StateImpl<string>(SourceContext.Current, Feed.Async(async ct => "AStateProperty"));

	public IFeed<MyRecord> ARecordFeedProperty { get; } = Feed.Async(async ct => new MyRecord("ARecordFeedProperty", 42, default, new MyWeirdRecord()));

	public IState<MyRecord> ARecordStateProperty { get; } = State<MyRecord>.Async(async ct => new MyRecord("ARecordStateProperty", 42, default, new MyWeirdRecord()));

	public IListFeed<string> AListFeedProperty { get; } = Feed.Async(async ct => new[] { "AListFeedProperty_1", "AListFeedProperty_2" }.ToImmutableList()).AsListFeed();

	public IListState<string> AListStateProperty { get; } = new ListStateImpl<string>(new StateImpl<IImmutableList<string>>(SourceContext.Current, Feed.Async(async ct => new[] { "AListStateProperty_1", "AListStateProperty_2" }.ToImmutableList() as IImmutableList<string>)));

	public CustomFeed ACustomFeedProperty { get; } = new CustomFeed();

	public void AParameterLessMethod() { }

	public void AParameterizedMethod(string arg1, int arg2) { }

	public (string result1, int result2) AParameterLessMethodReturningATuple() => ("AParameterLessMethodReturningATuple", 42);

	public (string result1, int result2) AParameterizedMethodReturningATuple(string arg1, int arg2) => (arg1, arg2);
}

public record MyRecord(string Property1, int Property2, MySubRecord? Property3, MyWeirdRecord Property4);

public record MySubRecord(string Prop1, int Prop2);

public record MyWeirdRecord
{
	public string ReadWriteProperty { get; set; } = "ReadWriteProperty";

	public string ReadInitProperty { get; init; } = "ReadInitProperty";

	public string ReadOnlyProperty { get; } = "ReadOnlyProperty";

	public string WriteOnlyProperty { set { } }

	public string InitOnlyProperty { init { } }

	public string? ANullableProperty { get; }
}

public record MyRecordWithAValueProperty(string Value);

public class CustomFeed : IFeed<string>
{
	/// <inheritdoc />
	public async IAsyncEnumerable<Message<string>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		yield break;
	}
}
