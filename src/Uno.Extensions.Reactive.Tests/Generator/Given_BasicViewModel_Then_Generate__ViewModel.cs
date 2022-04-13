using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbrella.Presentation.Feeds.Collections;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests.Generator;

[ReactiveBindable(false)]
public partial class MyListFeedTestViewModel
{
	public MyListFeedTestViewModel(
		IListFeed<MyRecord> anExternalInput, // External source, we don't do anything with it
		IListState<MyRecord> anExternalReadWriteInput, // External source, we don't do anything with it

		IInput<MyRecord> aBindableSingleInput,
		IListInput<MyRecord> aBindableInput // We generate a bindable collection of denormalized MyRecord => Allow Add(MyRecord) AND Add(BindableMyRecord) (drag drop)
		)
	{
		aBindableSingleInput.UpdateValue(i => i.SomeOrDefault(new MyRecord("",0, default!, default!))! with { Property1 = "hello" }, CancellationToken.None);

		aBindableInput.UpdateValue(items => AddFavorite(items), CancellationToken.None);

		// Projection sur un input List feed
		//aBindableInput.Select(items => items?.Where(r => r.Property1 is not null and not { Length: 0 }));
		aBindableInput.Select(item => item?.Property1);


		// ######## Projection sur un list feed custom (API => Pas de state) ########
		var myCustomListFeed = default(IFeed<CustomCollection<MyRecord>>);

		//var abc = myCustomListFeed.Select(items => new CustomCollection<string>());
		//ListFeed<CustomCollection<string>, string> def = abc;
		//TestFeed<CustomCollection<string>> ghi = abc;
		//IFeed<CustomCollection<string>> jkl = abc;

		//.AsListFeed().Select(str => str.Count);
		// Si on n'a pas de support de .Select sur les custom collection ça ne me semble pas dramatique ?
		// De toute façon on va perdre le type de collection en sortie ...
		myCustomListFeed.AsListFeed().Select(item => item?.Property1);
	}

	private Option<IImmutableList<MyRecord>> AddFavorite(Option<IImmutableList<MyRecord>> items)
	{
		if (items.IsNone() || (items.IsSome(out var its) && its is {Count:0}))
		{
			return Option.None<IImmutableList<MyRecord>>();
		}

		return items.SomeOrDefault()?.Add(new MyRecord("prop1", 42, default!, default!)).AsOption();
	}

	public ListFeed<MyRecord> MyListFeed { get; }


	partial class BindableGiven_BasicViewModel_Then_Generate__ViewModel : Bin
	{
		public BindableGiven_BasicViewModel_Then_Generate__ViewModel()
		{
			var vm = new MyListFeedTestViewModel(null!, null!, null!, null!);
			var ctx = global::Uno.Extensions.Reactive.Core.SourceContext.GetOrCreate(vm);

			var abc = new ListState<string>(null!);



			ctx.GetOrCreateState(vm.MyListFeed.AsFeed() ?? throw new NullReferenceException("The feed field 'AFeedField' is null. Public feeds fields must be initialized in the constructor."));
		}
	}
}

public class BindableListState<T> : IState<ICollectionView>
{
	private readonly IListState<T> _source;
	private readonly BindableCollection _view = BindableCollection.CreateUntyped();

	public BindableListState(IListState<T> source)
	{
		_source = source;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		await foreach (var message in _source.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (message.Changes.Contains(MessageAxis.Data))
			{
				_view.Switch();

				// If only the Data has changes, we ignore the message
				if (message.Changes is { Count: 1 })
				{
					continue;
				}
			}

			yield return new Message<IImmutableList<T>>()
		}
	}

	/// <inheritdoc />
	public ValueTask Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> throw new NotImplementedException();
}

public class CustomCollection<T> : IImmutableList<T>
{
	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
		=> throw new NotImplementedException();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	/// <inheritdoc />
	public int Count { get; }

	/// <inheritdoc />
	public T this[int index] => throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> Add(T value)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> AddRange(IEnumerable<T> items)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> Clear()
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> Insert(int index, T element)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> InsertRange(int index, IEnumerable<T> items)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> RemoveAll(Predicate<T> match)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> RemoveAt(int index)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> RemoveRange(int index, int count)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
		=> throw new NotImplementedException();

	/// <inheritdoc />
	public IImmutableList<T> SetItem(int index, T value)
		=> throw new NotImplementedException();
}

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

	public Given_BasicViewModel_Then_Generate__ViewModel(
		short aParameterToNotBeAParameterLessCtor1,
		IInput<string> anInput,
		IInput<string> aReadWriteInput,
		IInput<MyRecord> aRecordInput,
		IInput<MyWeirdRecord> aWeirdRecordInput,
		IInput<MyRecordWithAValueProperty> aRecordWithAValuePropertyInput,
		IInput<int> anInputConflictingWithAProperty)
		//IState<float> testState)
	{
		Assert.IsNotNull(anInput);
		Assert.IsNotNull(aReadWriteInput);
		Assert.IsNotNull(aRecordInput);
		Assert.IsNotNull(aWeirdRecordInput);
		Assert.IsNotNull(aRecordWithAValuePropertyInput);
		Assert.IsNotNull(anInputConflictingWithAProperty);
	}

	public Given_BasicViewModel_Then_Generate__ViewModel(
		int aParameterToNotBeAParameterLessCtor2,
		ICommandBuilder aTriggerInput,
		ICommandBuilder<string> aTypedTriggerInput)
	{
		Assert.IsNotNull(aTriggerInput);
		Assert.IsNotNull(aTypedTriggerInput);
	}

	public string AnInputConflictingWithAProperty { get; } = "AnInputConflictingWithAProperty";

	public string AField = "AField";

	internal string AnInternalField = "AnInternalField";

	protected internal string AProtectedInternalField = "AProtectedInternalField";

	public IFeed<string> AFeedField = Feed.Async(async ct => "42");

	public IState<string> AStateField = new State<string>(SourceContext.Current, Feed.Async(async ct => "42"));

	public CustomFeed ACustomFeedField = new CustomFeed();

	public string AProperty { get; set; } = "AProperty";

	internal string AnInternalProperty { get; set; } = "AnInternalProperty";

	protected internal string AProtectedInternalProperty { get; set; } = "AProtectedInternalProperty";

	public string AReadOnlyProperty { get; } = nameof(AReadOnlyProperty);

	public string ASetOnlyProperty { set { } }

	public IFeed<string> AFeedProperty { get; } = Feed.Async(async ct => "AFeedProperty");

	public IState<string> AStateProperty { get; } = new State<string>(SourceContext.Current, Feed.Async(async ct => "AStateProperty"));

	public CustomFeed ACustomFeedProperty { get; } = new CustomFeed();

	public void AParameterLessMethod() { }

	public void AParameterizedMethod(string arg1, int arg2) { }

	public (string result1, int result2) AParameterLessMethodReturningATuple() => ("AParameterLessMethodReturningATuple", 42);

	public (string result1, int result2) AParameterizedMethodReturningATuple(string arg1, int arg2) => (arg1, arg2);
}

public record MyRecord(string Property1, int Property2, MySubRecord Property3, MyWeirdRecord Property4);

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
