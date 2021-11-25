#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Tests.Generator;

public partial class Given_BasicViewModel_Then_Generate__ViewModel
{
	public Given_BasicViewModel_Then_Generate__ViewModel()
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(string aRandomService)
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(IFeed<string> anInput, IState<string> aReadWriteInput)
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(IFeed<MyRecord> aRecordInput, IFeed<MyWeirdRecord> aWeirdRecordInput)
	{
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(int aParameterToNotBeAParameterLessCtor, ICommandBuilder aTriggerInput, ICommandBuilder<string> aTypedTriggerInput)
	{
	}

	public string AField;

	internal string AnInternalField;

	protected internal string AProtectedInternalField;

	public IFeed<string> AFeedField = default!;

	public IState<string> AStateField = default!;

	public CustomFeed ACustomFeedField = default!;

	public string AProperty { get; set; }

	internal string AnInternalProperty { get; set; }

	protected internal string AProtectedInternalProperty { get; set; }

	public string AReadOnlyProperty { get; }

	public string ASetOnlyProperty { set { } }

	public IFeed<string> AFeedProperty { get; } = default!;

	public IState<string> AStateProperty { get; } = default!;

	public CustomFeed ACustomFeedProperty { get; } = default!;

	public void AParameterLessMethod() { }

	public void AParameterizedMethod(string arg1, int arg2) { }

	public (string result1, int result2) AParameterLessMethodReturningATuple() => default;

	public (string result1, int result2) AParameterizedMethodReturningATuple() => default;
}

public record MyRecord(string Property1, int Property2, MySubRecord Property3, MyWeirdRecord Property4);

public record MySubRecord(string Prop1, int Prop2);

public record MyWeirdRecord
{
	public string ReadWriteProperty { get; set; }

	public string ReadInitProperty { get; init; }

	public string ReadOnlyProperty { get; }

	public string WriteOnlyProperty { set { } }

	public string InitOnlyProperty { init { } }

#nullable enable
	public string? ANullableProperty { get; }
#nullable disable
}

public class CustomFeed : IFeed<string>
{
	/// <inheritdoc />
	public IAsyncEnumerable<Message<string>> GetSource(SourceContext context, CancellationToken ct = default)
		=> throw new NotImplementedException();
}
