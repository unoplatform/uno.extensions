using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Uno.Extensions.Reactive.Testing;
using KeyAttribute = Uno.Extensions.Equality.KeyAttribute;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_CoreListStateOperators : FeedTests
{
	#region UpdateAsync
	[TestMethod]
	[ExpectedException(typeof(NullReferenceException))] // Note: This is a compilation tests!
	public async Task When_UpdateAsync_Then_AcceptsNotNullAndStruct()
	{
		await default(IListState<int>)!.UpdateAsync(_ => ImmutableList.Create(42), CT);
		await default(IListState<int>)!.UpdateAsync(_ => default(IImmutableList<int>), CT);
		await default(IListState<int?>)!.UpdateAsync(_ => ImmutableList.Create<int?>(42), CT);
		await default(IListState<int?>)!.UpdateAsync(_ => default(IImmutableList<int?>), CT);
		await default(IListState<string>)!.UpdateAsync(_ => ImmutableList.Create(""), CT);
		await default(IListState<string>)!.UpdateAsync(_ => default(IImmutableList<string>), CT);
		await default(IListState<string?>)!.UpdateAsync(_ => ImmutableList.Create<string?>(""), CT);
		await default(IListState<string?>)!.UpdateAsync(_ => default(IImmutableList<string?>), CT);
		await default(IListState<MyStruct>)!.UpdateAsync(_ => ImmutableList.Create<MyStruct>(new MyStruct()), CT);
		await default(IListState<MyStruct>)!.UpdateAsync(_ => default(IImmutableList<MyStruct>), CT);
		await default(IListState<MyStruct?>)!.UpdateAsync(_ => ImmutableList.Create<MyStruct?>(new MyStruct()), CT);
		await default(IListState<MyStruct?>)!.UpdateAsync(_ => default(IImmutableList<MyStruct?>), CT);
		await default(IListState<MyClass>)!.UpdateAsync(_ => ImmutableList.Create<MyClass>(new MyClass()), CT);
		await default(IListState<MyClass>)!.UpdateAsync(_ => default(IImmutableList<MyClass>), CT);
		await default(IListState<MyClass?>)!.UpdateAsync(_ => ImmutableList.Create<MyClass?>(new MyClass()), CT);
		await default(IListState<MyClass?>)!.UpdateAsync(_ => default(IImmutableList<MyClass?>), CT);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create(42);
		var state = ListState<int>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create(42);
		var state = ListState<int>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<int>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfNullableInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int?>(42);
		var state = ListState<int?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfNullableInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int?>(42);
		var state = ListState<int?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<int?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string>("42");
		var state = ListState<string>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string>("42");
		var state = ListState<string>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<string>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfNullableString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string?>("42");
		var state = ListState<string?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfNullableString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string?>("42");
		var state = ListState<string?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<string?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct>(new MyStruct());
		var state = ListState<MyStruct>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct>(new MyStruct());
		var state = ListState<MyStruct>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<MyStruct>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfNullableStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct?>(new MyStruct());
		var state = ListState<MyStruct?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfNullableStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct?>(new MyStruct());
		var state = ListState<MyStruct?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<MyStruct?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass>(new MyClass());
		var state = ListState<MyClass>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None));
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass>(new MyClass());
		var state = ListState<MyClass>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<MyClass>.Empty, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None));
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullListStateOfNullableObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass?>(new MyClass());
		var state = ListState<MyClass?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None));
	}

	[TestMethod]
	public async Task When_UpdateAsyncEmptyListStateOfNullableObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass?>(new MyClass());
		var state = ListState<MyClass?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateAsync(_ => ImmutableList<MyClass?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None));
	}
	#endregion

	#region UpdateDataAsync
	[TestMethod]
	[ExpectedException(typeof(NullReferenceException))] // Note: This is a compilation tests!
	public async Task When_UpdateDataAsync_Then_AcceptsNotNullAndStruct()
	{
		await default(IListState<int>)!.UpdateDataAsync(_ => ImmutableList.Create(42), CT);
		await default(IListState<int>)!.UpdateDataAsync(_ => default(ImmutableList<int>)!, CT);
		await default(IListState<int?>)!.UpdateDataAsync(_ => ImmutableList.Create<int?>(42), CT);
		await default(IListState<int?>)!.UpdateDataAsync(_ => default(ImmutableList<int?>)!, CT);
		await default(IListState<string>)!.UpdateDataAsync(_ => ImmutableList.Create(""), CT);
		await default(IListState<string>)!.UpdateDataAsync(_ => default(ImmutableList<string>)!, CT);
		await default(IListState<string?>)!.UpdateDataAsync(_ => ImmutableList.Create<string?>(""), CT);
		await default(IListState<string?>)!.UpdateDataAsync(_ => default(ImmutableList<string?>)!, CT);
		await default(IListState<MyStruct>)!.UpdateDataAsync(_ => ImmutableList.Create<MyStruct>(new MyStruct()), CT);
		await default(IListState<MyStruct>)!.UpdateDataAsync(_ => default(ImmutableList<MyStruct>)!, CT);
		await default(IListState<MyStruct?>)!.UpdateDataAsync(_ => ImmutableList.Create<MyStruct?>(new MyStruct()), CT);
		await default(IListState<MyStruct?>)!.UpdateDataAsync(_ => default(ImmutableList<MyStruct?>)!, CT);
		await default(IListState<MyClass>)!.UpdateDataAsync(_ => ImmutableList.Create<MyClass>(new MyClass()), CT);
		await default(IListState<MyClass>)!.UpdateDataAsync(_ => default(ImmutableList<MyClass>)!, CT);
		await default(IListState<MyClass?>)!.UpdateDataAsync(_ => ImmutableList.Create<MyClass?>(new MyClass()), CT);
		await default(IListState<MyClass?>)!.UpdateDataAsync(_ => default(ImmutableList<MyClass?>)!, CT);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int>(42);
		var state = ListState<int>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<int>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueListStateOfInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int>(42);
		var state = ListState<int>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<int>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int>(42);
		var state = ListState<int>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<int>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfNullableInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int?>(42);
		var state = ListState<int?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<int?>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullListStateOfNullableInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int?>(42);
		var state = ListState<int?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<int?>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfNullableInt_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<int?>(42);
		var state = ListState<int?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<int?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string>("42");
		var state = ListState<string>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<string>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueListStateOfString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string>("42");
		var state = ListState<string>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<string>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string>("42");
		var state = ListState<string>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<string>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfNullableString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string?>("42");
		var state = ListState<string?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<string?>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullListStateOfNullableString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string?>("42");
		var state = ListState<string?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<string?>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfNullableString_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<string?>("42");
		var state = ListState<string?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<string?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct>(new MyStruct());
		var state = ListState<MyStruct>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<MyStruct>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueListStateOfStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct>(new MyStruct());
		var state = ListState<MyStruct>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<MyStruct>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct>(new MyStruct());
		var state = ListState<MyStruct>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<MyStruct>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfNullableStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct?>(new MyStruct());
		var state = ListState<MyStruct?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<MyStruct?>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullListStateOfNullableStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct?>(new MyStruct());
		var state = ListState<MyStruct?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<MyStruct?>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfNullableStruct_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyStruct?>(new MyStruct());
		var state = ListState<MyStruct?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<MyStruct?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass>(new MyClass());
		var state = ListState<MyClass>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<MyClass>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueListStateOfObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass>(new MyClass());
		var state = ListState<MyClass>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<MyClass>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass>(new MyClass());
		var state = ListState<MyClass>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<MyClass>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionListStateOfNullableObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass?>(new MyClass());
		var state = ListState<MyClass?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<IImmutableList<MyClass?>>), CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullListStateOfNullableObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass?>(new MyClass());
		var state = ListState<MyClass?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(ImmutableList<MyClass?>)!, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsEmptyListStateOfNullableObject_Then_TreatAsNone()
	{
		var value = ImmutableList.Create<MyClass?>(new MyClass());
		var state = ListState<MyClass?>.Value(this, () => value);
		var result = state.Record();

		await state.UpdateDataAsync(_ => ImmutableList<MyClass?>.Empty, CT);

		result.Should().Be(m => m
			.Message(value)
			.Message(Data.None)
		);
	}
	#endregion

	#region InsertAsync
	[TestMethod]
	public async Task WhenInsertAsync_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(42));
		var result = sut.Record();

		await sut.InsertAsync(43, CT);

		result.Should().Be(m => m
			.Message(Items.Some(42))
			.Message(Items.Some(43, 42))
		);
	}
	#endregion

	#region AddAsync
	[TestMethod]
	public async Task WhenAddAsync_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(42));
		var result = sut.Record();

		await sut.AddAsync(43, CT);

		result.Should().Be(m => m
			.Message(Items.Some(42))
			.Message(Items.Some(42, 43))
		);
	}
	#endregion

	#region RemoveAllAsync
	[TestMethod]
	public async Task WhenRemoveAllAsync_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		await sut.RemoveAllAsync(i => i == 42, CT);

		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 43))
		);
	}

	[TestMethod]
	public async Task WhenRemoveAllAsyncRemovesAllItems_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		await sut.RemoveAllAsync(i => true, CT);

		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Data.None)
		);
	}
	#endregion

	#region UpdateAllAsync
	[TestMethod]
	public async Task WhenUpdateAllAsync_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		await sut.UpdateAllAsync(i => i == 42, i => i * 2, CT);

		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 84, 43))
		);
	}
	#endregion

	#region UpdateItemAsync
	[TestMethod]
	public async Task WhenUpdateItemAsync_WithValue_Then_ItemAdded()
	{
		LeItem itemToUpdate = new(2, 42);
		var sut = ListState.Value(this, () => ImmutableList.Create<LeItem>(new(1,41), itemToUpdate, new(3, 43)));
		var result = sut.Record();

		await sut.UpdateItemAsync(itemToUpdate, new LeItem(2, 84), CT);

		result.Should().Be(m => m
			.Message(Items.Some(new LeItem(1, 41), new LeItem(2, 42), new LeItem(3, 43)))
			.Message(Items.Some(new LeItem(1, 41), new LeItem(2, 84), new LeItem(3, 43)))
		);
	}

	[TestMethod]
	public async Task WhenUpdateItemAsync_WithUpdater_Then_ItemAdded()
	{
		LeItem itemToUpdate = new(3, 43);
		var sut = ListState.Value(this, () => ImmutableList.Create<LeItem>(new(1, 41), new (2, 42), itemToUpdate));
		var result = sut.Record();

		await sut.UpdateItemAsync(itemToUpdate, i => new(3, i.Version * 3), CT);

		result.Should().Be(m => m
			.Message(Items.Some(new LeItem(1, 41), new LeItem(2, 42), new LeItem(3, 43)))
			.Message(Items.Some(new LeItem(1, 41), new LeItem(2, 42), new LeItem(3, 129)))
		);
	}
	#endregion

	#region UpdateAsync
	[TestMethod]
	public async Task WhenUpdateAsync_Then_ItemAdded()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(new MyItem(42, 0)));
		var result = sut.Record();

		await sut.UpdateAsync(new MyItem(42, 1), CT);

		result.Should().Be(m => m
			.Message(Items.Some(new MyItem(42, 0)))
			.Message(Items.Some(new MyItem(42, 1)))
		);
	}
	#endregion

	#region TryDeselectAsync
	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_Then_ItemDeselected()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45));
		var result = sut.Record();

		// Select multiple items first
		await sut.TrySelectAsync(ImmutableList.Create(41, 42, 43), CT);

		// Deselect one item
		var count = await sut.TryDeselectAsync(42, CT);

		count.Should().Be(1);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 42, 43))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 43))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_NotSelected_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45));
		var result = sut.Record();

		// Select some items
		await sut.TrySelectAsync(ImmutableList.Create(41, 43), CT);

		// Try to deselect an item that's not selected
		var count = await sut.TryDeselectAsync(42, CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 43))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_NotInList_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		// Select some items
		await sut.TrySelectAsync(ImmutableList.Create(41, 42), CT);

		// Try to deselect an item that's not in the list
		var count = await sut.TryDeselectAsync(99, CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 42, 43), Selection.Items(41, 42))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_EmptySelection_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		// Try to deselect when nothing is selected
		var count = await sut.TryDeselectAsync(42, CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_EmptyList_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList<int>.Empty);
		var result = sut.Record();

		// Try to deselect from empty list
		var count = await sut.TryDeselectAsync(42, CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_SingleItem_LastSelectedItem_Then_SelectionBecomesEmpty()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		// Select only one item
		await sut.TrySelectAsync(42, CT);

		// Deselect the only selected item
		var count = await sut.TryDeselectAsync(42, CT);

		count.Should().Be(1);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 42, 43), Selection.Items(42))
			.Message(Items.Some(41, 42, 43), Selection.Empty)
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_Then_ItemsDeselected()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45));
		var result = sut.Record();

		// Select all items
		await sut.TrySelectAsync(ImmutableList.Create(41, 42, 43, 44, 45), CT);

		// Deselect multiple items
		var count = await sut.TryDeselectAsync(ImmutableList.Create(42, 44), CT);

		count.Should().Be(2);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 43, 45))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_SomeNotSelected_Then_DeselectsOnlySelectedOnes()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45));
		var result = sut.Record();

		// Select some items
		await sut.TrySelectAsync(ImmutableList.Create(41, 43, 45), CT);

		// Try to deselect mix of selected and unselected items
		var count = await sut.TryDeselectAsync(ImmutableList.Create(42, 43, 44), CT);

		count.Should().Be(1);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 43, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 45))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_NoneSelected_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45));
		var result = sut.Record();

		// Select some items
		await sut.TrySelectAsync(ImmutableList.Create(41, 45), CT);

		// Try to deselect items that are not selected
		var count = await sut.TryDeselectAsync(ImmutableList.Create(42, 43, 44), CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45))
			.Message(Items.Some(41, 42, 43, 44, 45), Selection.Items(41, 45))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_EmptyList_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList<int>.Empty);
		var result = sut.Record();

		// Try to deselect from empty list
		var count = await sut.TryDeselectAsync(ImmutableList.Create(41, 42), CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_NullList_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		await sut.TrySelectAsync(ImmutableList.Create(41, 42), CT);

		// Try to deselect with null list
		var count = await sut.TryDeselectAsync(null!, CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 42, 43), Selection.Items(41, 42))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_EmptySelection_Then_ReturnsZero()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		// Try to deselect when nothing is selected
		var count = await sut.TryDeselectAsync(ImmutableList.Create(41, 42), CT);

		count.Should().Be(0);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_MultipleItems_AllSelectedItems_Then_SelectionBecomesEmpty()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
		var result = sut.Record();

		// Select items
		await sut.TrySelectAsync(ImmutableList.Create(41, 43), CT);

		// Deselect all selected items
		var count = await sut.TryDeselectAsync(ImmutableList.Create(41, 43), CT);

		count.Should().Be(2);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43))
			.Message(Items.Some(41, 42, 43), Selection.Items(41, 43))
			.Message(Items.Some(41, 42, 43), Selection.Empty)
		);
	}

	[TestMethod]
	public async Task WhenTryDeselectAsync_ConsecutiveItems_Then_RangesHandledCorrectly()
	{
		var sut = ListState.Value(this, () => ImmutableList.Create(41, 42, 43, 44, 45, 46, 47));
		var result = sut.Record();

		// Select a range
		await sut.TrySelectAsync(ImmutableList.Create(42, 43, 44, 45, 46), CT);

		// Deselect middle items
		var count = await sut.TryDeselectAsync(ImmutableList.Create(43, 44, 45), CT);

		count.Should().Be(3);
		result.Should().Be(m => m
			.Message(Items.Some(41, 42, 43, 44, 45, 46, 47))
			.Message(Items.Some(41, 42, 43, 44, 45, 46, 47), Selection.Items(42, 43, 44, 45, 46))
			.Message(Items.Some(41, 42, 43, 44, 45, 46, 47), Selection.Items(42, 46))
		);
	}
	#endregion

	private record class MyClass;
	private record struct MyStruct;

	internal partial record class MyItem(int Id, int Version);

	public partial record LeItem([property: Key] int Id, int Version);
}
