using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

[TestClass]
public class Given_Bindable : FeedUITests
{
	private TestViewModel _provider = default!;

	[TestInitialize]
	public override void Initialize()
	{
		base.Initialize();

		_provider = new TestViewModel(this);
	}

	[TestMethod]
	public async Task When_SetDefaultIntDuringSyncInit_Then_NoPropertyChange()
		=> await When_SetValueDuringSyncInit<int>();

	[TestMethod]
	public async Task When_SetDefaultEnumDuringSyncInit_Then_NoPropertyChange()
		=> await When_SetValueDuringSyncInit<TestEnum>();

	[TestMethod]
	public async Task When_SetDefaultStructDuringSyncInit_Then_NoPropertyChange()
		=> await When_SetValueDuringSyncInit<MyStruct>();

	[TestMethod]
	public async Task When_SetDefaultObjectDuringSyncInit_Then_NoPropertyChange()
		=> await When_SetValueDuringSyncInit<object>();

	[TestMethod]
	public async Task When_SetIntDuringSyncInit_Then_PropertyChange()
		=> await When_SetValueDuringSyncInit(0, 42, 1);

	[TestMethod]
	public async Task When_SetEnumDuringSyncInit_Then_PropertyChange()
		=> await When_SetValueDuringSyncInit(TestEnum.A, TestEnum.B, 1);

	[TestMethod]
	public async Task When_SetStructDuringSyncInit_Then_PropertyChange()
		=> await When_SetValueDuringSyncInit(new MyStruct(0), new MyStruct(42), 1);

	[TestMethod]
	public async Task When_SetObjectDuringSyncInit_Then_PropertyChange()
		=> await When_SetValueDuringSyncInit(new object(), new object(), 1); // We use ref-equality for object to avoid deep comparison on the UI thread


	[TestMethod]
	public async Task When_SetSameIntAfterAsyncInit_Then_NoPropertyChange()
		=> await When_SetValueAfterAsyncInit<int>();

	[TestMethod]
	public async Task When_SetSameEnumAfterAsyncInit_Then_NoPropertyChange()
		=> await When_SetValueAfterAsyncInit<TestEnum>();

	[TestMethod]
	public async Task When_SetSameStructAfterAsyncInit_Then_NoPropertyChange()
		=> await When_SetValueAfterAsyncInit<MyStruct>();

	[TestMethod]
	public async Task When_SetSameObjectAfterAsyncInit_Then_NoPropertyChange()
		=> await When_SetValueAfterAsyncInit<object>();

	[TestMethod]
	public async Task When_SetIntAfterAsyncInit_Then_PropertyChange()
		=> await When_SetValueAfterAsyncInit(0, 42, 1);

	[TestMethod]
	public async Task When_SetEnumAfterAsyncInit_Then_PropertyChange()
		=> await When_SetValueAfterAsyncInit(TestEnum.A, TestEnum.B, 1);

	[TestMethod]
	public async Task When_SetStructAfterAsyncInit_Then_PropertyChange()
		=> await When_SetValueAfterAsyncInit(new MyStruct(0), new MyStruct(42), 1);

	[TestMethod]
	public async Task When_SetObjectAfterAsyncInit_Then_PropertyChange()
		=> await When_SetValueAfterAsyncInit(new object(), new object(), 1); // We use ref-equality for object to avoid deep comparison on the UI thread

	private async Task When_SetValueDuringSyncInit<T>(T original = default!, T updated = default!, int expectedCount = 0)
	{
		var (property, state) = _provider.GetSync(original);
		var sut = new Bindable<T>(property);
		var changes = 0;

		await Dispatcher.ExecuteAsync(() =>
		{
			sut.PropertyChanged += (snd, e) => changes++;
			sut.SetValue(updated);
		});

		changes.Should().Be(expectedCount);
	}

	private async Task When_SetValueAfterAsyncInit<T>(T original = default!, T updated = default!, int expectedCount = 0)
	{
		var evt = new ManualResetEvent(false);
		var (property, state) = _provider.GetAsync(async ct =>
		{
			if (!evt.WaitOne(FeedRecorder.DefaultTimeout, false))
			{
				throw new InvalidOperationException();
			}
			return original;
		});
		var sut = new Bindable<T>(property);
		var changes = 0;

		await Dispatcher.ExecuteAsync(async ct =>
		{
			sut.PropertyChanged += (snd, e) => changes++; // 1 init subscription to the underling state by subscribing to the PropertyChanged event from the UI thread.
			evt.Set(); // 2 release the async initialization
			await sut.GetSource(_provider.Owner.Context).FirstAsync(ct); // 3 Wait for init to reach the UI thread.
			sut.SetValue(updated); // 4 Finally set the value from the UI thread, just like a property change by a control using 2-way binding
		});

		changes.Should().Be(expectedCount);
	}

	private enum TestEnum
	{
		A,
		B,
		C
	}

	private record struct MyStruct(int Value);

	private class TestViewModel(Given_Bindable owner) : BindableViewModelBase
	{
		public Given_Bindable Owner => owner;

		public (BindablePropertyInfo<T> property, IState<T> state) GetSync<T>(T defaultValue = default(T)!, [CallerMemberName] string name = "")
		{
			var backingState = State<T>.Value(owner, () => defaultValue);

			return (base.Property(name, backingState), backingState);
		}

		public (BindablePropertyInfo<T> property, IState<T> state) GetAsync<T>(AsyncFunc<T> defaultValue, [CallerMemberName] string name = "")
		{
			var backingState = State<T>.Async(owner, defaultValue);

			return (base.Property(name, backingState), backingState);
		}

		/// <inheritdoc />
		protected override void __Reactive_UpdateModel(object updatedModel)
			=> throw new NotSupportedException("Hot reload not supported in test!");
	}
}
