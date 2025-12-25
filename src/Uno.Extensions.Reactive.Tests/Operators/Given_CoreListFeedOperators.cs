using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_CoreListFeedOperators
{
	#region GetAwaiter
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_GetAwaiter_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref

		var intValue = await default(IListFeed<int>)!;
		var nullableIntValue = await default(IListFeed<int?>)!;

		var stringValue = await default(IListFeed<string>)!;
		var nullableStringValue = await default(IListFeed<string?>)!;

		var structValue = await default(IListFeed<MyStruct>)!;
		var nullableStructValue = await default(IListFeed<MyStruct?>)!;

		var classValue = await default(IListFeed<MyClass>)!;
		var nullableClassValue = await default(IListFeed<MyClass?>)!;
	}

	[TestMethod]
	public async Task When_GetAwaiter_Then_GetValue()
	{
		(await Feed.Dynamic(async ct => ImmutableList.Create(42)).AsListFeed()).Should().BeEquivalentTo(new[] { 42 });
	}

	[TestMethod]
	[DataRow(AsyncFeedValue.Default, 42)]
	[DataRow(AsyncFeedValue.AllowTransient, 41)]
	[DataRow(AsyncFeedValue.AllowError, 42)]
	[DataRow(AsyncFeedValue.All, 41)]
	public async Task When_ValueWithTransient_Then_GetValue(AsyncFeedValue kind, int expected)
		=> (await GetFeedWithTransient().Value(kind)).Should().BeEquivalentTo(new[] { expected });

	[TestMethod]
	[DataRow(AsyncFeedValue.Default, -1, true)]
	[DataRow(AsyncFeedValue.AllowTransient, -1, true)]
	[DataRow(AsyncFeedValue.AllowError, 41, false)]
	[DataRow(AsyncFeedValue.All, 41, false)]
	public async Task When_ValueWithError_Then_GetValue(AsyncFeedValue kind, int expected, bool expectError)
	{
		var gotException = false;
		try
		{
			(await GetFeedWithError().Value(kind)).Should().BeEquivalentTo(new[] { expected });
		}
		catch (TestException) when (expectError)
		{
			gotException = true;
		}

		if (expectError && !gotException)
		{
			Assert.Fail("Didn't get expected exception.");
		}
	}

	[TestMethod]
	[DataRow(AsyncFeedValue.Default, 42, false)]
	[DataRow(AsyncFeedValue.AllowTransient, -1, true)]
	[DataRow(AsyncFeedValue.AllowError, 42, false)]
	[DataRow(AsyncFeedValue.All, 41, false)]
	public async Task When_ValueWithTransientAndError_Then_GetValue(AsyncFeedValue kind, int expected, bool expectError)
	{
		var gotException = false;
		try
		{
			(await GetFeedWithTransientAndError().Value(kind)).Should().BeEquivalentTo(new[] { expected });
		}
		catch (TestException) when (expectError)
		{
			gotException = true;
		}

		if (expectError && !gotException)
		{
			Assert.Fail("Didn't get expected exception.");
		}
	}

	[TestMethod]
	[DataRow(AsyncFeedValue.Default, -1, true)]
	[DataRow(AsyncFeedValue.AllowTransient, -1, true)]
	[DataRow(AsyncFeedValue.AllowError, 42, false)]
	[DataRow(AsyncFeedValue.All, 41, false)]
	public async Task When_ValueWithTransientAndError_2_Then_GetValue(AsyncFeedValue kind, int expected, bool expectError)
	{
		var gotException = false;
		try
		{
			(await GetFeedWithTransientAndError(true).Value(kind)).Should().BeEquivalentTo(new[] { expected });
		}
		catch (TestException) when (expectError)
		{
			gotException = true;
		}

		if (expectError && !gotException)
		{
			Assert.Fail("Didn't get expected exception.");
		}
	}
	#endregion

	#region Value
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Value_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = await default(IListFeed<int>)!.Value();
		var nullableIntValue = await default(IListFeed<int?>)!.Value();

		var stringValue = await default(IListFeed<string>)!.Value();
		var nullableStringValue = await default(IListFeed<string?>)!.Value();

		var structValue = await default(IListFeed<MyStruct>)!.Value();
		var nullableStructValue = await default(IListFeed<MyStruct?>)!.Value();

		var classValue = await default(IListFeed<MyClass>)!.Value();
		var nullableClassValue = await default(IListFeed<MyClass?>)!.Value();
	}

	[TestMethod]
	public async Task When_Values_Then_AcceptsNotNullAndStruct()
	{
		var intValue = default(IListFeed<int>)!.Values();
		var nullableIntValue = default(IListFeed<int?>)!.Values();

		var stringValue = default(IListFeed<string>)!.Values();
		var nullableStringValue = default(IListFeed<string?>)!.Values();

		var structValue = default(IListFeed<MyStruct>)!.Values();
		var nullableStructValue = default(IListFeed<MyStruct?>)!.Values();

		var classValue = default(IListFeed<MyClass>)!.Values();
		var nullableClassValue = default(IListFeed<MyClass?>)!.Values();
	}

	[TestMethod]
	public async Task When_Value_Then_GetValue()
	{
		(await Feed.Dynamic(async ct => ImmutableList.Create(42)).AsListFeed().Value()).Should().BeEquivalentTo(new[] { 42 });
	}
	#endregion

	#region Data
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Data_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = await default(IListFeed<int>)!.Data();
		var nullableIntValue = await default(IListFeed<int?>)!.Data();

		var stringValue = await default(IListFeed<string>)!.Data();
		var nullableStringValue = await default(IListFeed<string?>)!.Data();

		var structValue = await default(IListFeed<MyStruct>)!.Data();
		var nullableStructValue = await default(IListFeed<MyStruct?>)!.Data();

		var classValue = await default(IListFeed<MyClass>)!.Data();
		var nullableClassValue = await default(IListFeed<MyClass?>)!.Data();
	}

	[TestMethod]
	public async Task When_DataSet_Then_AcceptsNotNullAndStruct()
	{
		var intValue = default(IListFeed<int>)!.DataSet();
		var nullableIntValue = default(IListFeed<int?>)!.DataSet();

		var stringValue = default(IListFeed<string>)!.DataSet();
		var nullableStringValue = default(IListFeed<string?>)!.DataSet();

		var structValue = default(IListFeed<MyStruct>)!.DataSet();
		var nullableStructValue = default(IListFeed<MyStruct?>)!.DataSet();

		var classValue = default(IListFeed<MyClass>)!.DataSet();
		var nullableClassValue = default(IListFeed<MyClass?>)!.DataSet();
	}
	#endregion

	#region Message
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Message_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = await default(IListFeed<int>)!.Message();
		var nullableIntValue = await default(IListFeed<int?>)!.Message();

		var stringValue = await default(IListFeed<string>)!.Message();
		var nullableStringValue = await default(IListFeed<string?>)!.Message();

		var structValue = await default(IListFeed<MyStruct>)!.Message();
		var nullableStructValue = await default(IListFeed<MyStruct?>)!.Message();

		var classValue = await default(IListFeed<MyClass>)!.Message();
		var nullableClassValue = await default(IListFeed<MyClass?>)!.Message();
	}

	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Messages_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = default(IListFeed<int>)!.Messages();
		var nullableIntValue = default(IListFeed<int?>)!.Messages();

		var stringValue = default(IListFeed<string>)!.Messages();
		var nullableStringValue = default(IListFeed<string?>)!.Messages();

		var structValue = default(IListFeed<MyStruct>)!.Messages();
		var nullableStructValue = default(IListFeed<MyStruct?>)!.Messages();

		var classValue = default(IListFeed<MyClass>)!.Messages();
		var nullableClassValue = default(IListFeed<MyClass?>)!.Messages();
	}
	#endregion

	private record class MyClass();
	private record struct MyStruct();

	private static IListFeed<int> GetFeedWithTransient()
	{
		async IAsyncEnumerable<Message<IImmutableList<int>>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<IImmutableList<int>>.Initial;
			yield return message = message.With().IsTransient(true).Data(ImmutableList.Create(41));
			yield return message = message.With().Data(ImmutableList.Create(42));
			yield return message = message.With().IsTransient(false);
		}

		return Feed.Create(GetMessages).AsListFeed();
	}

	private static IListFeed<int> GetFeedWithError()
	{
		async IAsyncEnumerable<Message<IImmutableList<int>>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<IImmutableList<int>>.Initial;
			yield return message = message.With().Error(new TestException()).Data(ImmutableList.Create(41));
			yield return message = message.With().Data(ImmutableList.Create(42));
			yield return message = message.With().Error(null);
		}

		return Feed.Create(GetMessages).AsListFeed();
	}

	private static IListFeed<int> GetFeedWithTransientAndError(bool finalBeforeClearError = false)
	{
		async IAsyncEnumerable<Message<IImmutableList<int>>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<IImmutableList<int>>.Initial;
			yield return message = message.With().IsTransient(true).Error(new TestException()).Data(ImmutableList.Create(41));
			yield return message = message.With().Data(ImmutableList.Create(42));
			if (finalBeforeClearError)
			{
				yield return message = message.With().IsTransient(false);
				yield return message = message.With().Error(null);
			}
			else
			{
				yield return message = message.With().Error(null);
				yield return message = message.With().IsTransient(false);
			}
		}

		return Feed.Create(GetMessages).AsListFeed();
	}
}
