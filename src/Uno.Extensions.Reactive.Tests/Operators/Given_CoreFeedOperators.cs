using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_CoreFeedOperators
{
	#region GetAwaiter
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_GetAwaiter_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref

		var intValue = await default(IFeed<int>)!;
		var nullableIntValue = await default(IFeed<int?>)!;

		var stringValue = await default(IFeed<string>)!;
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableStringValue = await default(IFeed<string?>)!;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore

		var structValue = await default(IFeed<MyStruct>)!;
		var nullableStructValue = await default(IFeed<MyStruct?>)!;

		var classValue = await default(IFeed<MyClass>)!;
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableClassValue = await default(IFeed<MyClass?>)!;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
	}

	[TestMethod]
	public async Task When_GetAwaiter_Then_GetValue()
	{
		(await Feed.Dynamic(async ct => 42)).Should().Be(42);
	}

	[TestMethod]
	[DataRow(AsyncFeedValue.Default, 42)]
	[DataRow(AsyncFeedValue.AllowTransient, 41)]
	[DataRow(AsyncFeedValue.AllowError, 42)]
	[DataRow(AsyncFeedValue.All, 41)]
	public async Task When_ValueWithTransient_Then_GetValue(AsyncFeedValue kind, int expected)
		=> (await GetFeedWithTransient().Value(kind)).Should().Be(expected);

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
			(await GetFeedWithError().Value(kind)).Should().Be(expected);
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
			(await GetFeedWithTransientAndError().Value(kind)).Should().Be(expected);
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
			(await GetFeedWithTransientAndError(true).Value(kind)).Should().Be(expected);
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
		var intValue = await default(IFeed<int>)!.Value();
		var nullableIntValue = await default(IFeed<int?>)!.Value();

		var stringValue = await default(IFeed<string>)!.Value();
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableStringValue = await default(IFeed<string?>)!.Value();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore

		var structValue = await default(IFeed<MyStruct>)!.Value();
		var nullableStructValue = await default(IFeed<MyStruct?>)!.Value();

		var classValue = await default(IFeed<MyClass>)!.Value();
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableClassValue = await default(IFeed<MyClass?>)!.Value();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
	}

	[TestMethod]
	public async Task When_Values_Then_AcceptsNotNullAndStruct()
	{
		var intValue = default(IFeed<int>)!.Values();
		var nullableIntValue = default(IFeed<int?>)!.Values();

		var stringValue = default(IFeed<string>)!.Values();
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableStringValue = default(IFeed<string?>)!.Values();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore

		var structValue = default(IFeed<MyStruct>)!.Values();
		var nullableStructValue = default(IFeed<MyStruct?>)!.Values();

		var classValue = default(IFeed<MyClass>)!.Values();
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		var nullableClassValue = default(IFeed<MyClass?>)!.Values();
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
	}

	[TestMethod]
	public async Task When_Value_Then_GetValue()
	{
		(await Feed.Dynamic(async ct => 42).Value()).Should().Be(42);
	}
	#endregion

	#region Data
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Data_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = await default(IFeed<int>)!.Data();
		var nullableIntValue = await default(IFeed<int?>)!.Data();

		var stringValue = await default(IFeed<string>)!.Data();
		var nullableStringValue = await default(IFeed<string?>)!.Data();

		var structValue = await default(IFeed<MyStruct>)!.Data();
		var nullableStructValue = await default(IFeed<MyStruct?>)!.Data();

		var classValue = await default(IFeed<MyClass>)!.Data();
		var nullableClassValue = await default(IFeed<MyClass?>)!.Data();
	}

	[TestMethod]
	public async Task When_DataSet_Then_AcceptsNotNullAndStruct()
	{
		var intValue = default(IFeed<int>)!.DataSet();
		var nullableIntValue = default(IFeed<int?>)!.DataSet();

		var stringValue = default(IFeed<string>)!.DataSet();
		var nullableStringValue = default(IFeed<string?>)!.DataSet();

		var structValue = default(IFeed<MyStruct>)!.DataSet();
		var nullableStructValue = default(IFeed<MyStruct?>)!.DataSet();

		var classValue = default(IFeed<MyClass>)!.DataSet();
		var nullableClassValue = default(IFeed<MyClass?>)!.DataSet();
	}
	#endregion

	#region Message
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Message_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = await default(IFeed<int>)!.Message();
		var nullableIntValue = await default(IFeed<int?>)!.Message();

		var stringValue = await default(IFeed<string>)!.Message();
		var nullableStringValue = await default(IFeed<string?>)!.Message();

		var structValue = await default(IFeed<MyStruct>)!.Message();
		var nullableStructValue = await default(IFeed<MyStruct?>)!.Message();

		var classValue = await default(IFeed<MyClass>)!.Message();
		var nullableClassValue = await default(IFeed<MyClass?>)!.Message();
	}

	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_Messages_Then_AcceptsNotNullAndStruct()
	{
		// Note: Those are compilation tests! Will always throw null ref
		var intValue = default(IFeed<int>)!.Messages();
		var nullableIntValue = default(IFeed<int?>)!.Messages();

		var stringValue = default(IFeed<string>)!.Messages();
		var nullableStringValue = default(IFeed<string?>)!.Messages();

		var structValue = default(IFeed<MyStruct>)!.Messages();
		var nullableStructValue = default(IFeed<MyStruct?>)!.Messages();

		var classValue = default(IFeed<MyClass>)!.Messages();
		var nullableClassValue = default(IFeed<MyClass?>)!.Messages();
	}
	#endregion

	private record class MyClass();
	private record struct MyStruct();

	private static IFeed<int> GetFeedWithTransient()
	{
		async IAsyncEnumerable<Message<int>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<int>.Initial;
			yield return message = message.With().IsTransient(true).Data(41);
			yield return message = message.With().Data(42);
			yield return message = message.With().IsTransient(false);
		}

		return Feed.Create(GetMessages);
	}

	private static IFeed<int> GetFeedWithError()
	{
		async IAsyncEnumerable<Message<int>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<int>.Initial;
			yield return message = message.With().Error(new TestException()).Data(41);
			yield return message = message.With().Data(42);
			yield return message = message.With().Error(null);
		}

		return Feed.Create(GetMessages);
	}

	private static IFeed<int> GetFeedWithTransientAndError(bool finalBeforeClearError = false)
	{
		async IAsyncEnumerable<Message<int>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			var message = Message<int>.Initial;
			yield return message = message.With().IsTransient(true).Error(new TestException()).Data(41);
			yield return message = message.With().Data(42);
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

		return Feed.Create(GetMessages);
	}
}
