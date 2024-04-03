using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Utils.Dispatching;

[TestClass]
public class Given_DispatcherLocal
{
	[TestMethod]
	public async Task When_GetValue_Then_CreatePerThread()
	{
		var current = default(IDispatcher?);
		using var ui1 = new TestDispatcher("ui1");
		using var ui2 = new TestDispatcher("ui2");

		var sut = new DispatcherLocal<string>(
			factory: d => d is TestDispatcher ui ? ui.Name : "background",
			schedulersProvider: () => current);

		CountValues(sut).Should().Be(0);

		current = ui1;
		sut.Value.Should().Be("ui1");
		CountValues(sut).Should().Be(1);

		current = ui2;
		sut.Value.Should().Be("ui2");
		CountValues(sut).Should().Be(2);

		current = null;
		sut.Value.Should().Be("background");
		CountValues(sut).Should().Be(3);
	}

	[TestMethod]
	public async Task When_CreateValueWhileEnumerating_Then_Lock()
	{
		var current = default(IDispatcher?);
		using var ui = new TestDispatcher("ui");
		var gate = new ManualResetEvent(false);

		var sut = new DispatcherLocal<string>(
			factory: d => d is TestDispatcher td ? td.Name : "background",
			schedulersProvider: () => current);

		// Start enumeration
		sut.Value.Should().Be("background"); // init bg value to get something to enumerate on
		var enumeration = Task.Run(() =>
		{
			var count = 0;
			sut.ForEachValue((d, _) =>
			{
				if (count is 0)
				{
					gate.WaitOne();
				}
				count++;
			});

			return count;
		});

		// Confirm that the enumeration is stuck
		await Task.Delay(100);
		enumeration.IsCompleted.Should().BeFalse();

		// As we are enumerating, we cannot create a new value
		var getValue = Task.Run(() =>
		{
			current = ui;

			return sut.Value;
		});

		// Confirm that the enumeration and get value are stuck
		await Task.Delay(100);
		enumeration.IsCompleted.Should().BeFalse();
		getValue.IsCompleted.Should().BeFalse();

		// Release enumeration
		gate.Set();

		// Confirm both task completed ... and enumeration was effectively run only on first value
		for (var i = 0; i < 100 && (!enumeration.IsCompleted || !getValue.IsCompleted); i++)
		{
			await Task.Delay(10);
		}
		enumeration.IsCompleted.Should().BeTrue();
		getValue.IsCompleted.Should().BeTrue();

		enumeration.Result.Should().Be(1);
	}

	[TestMethod]
	public async Task When_EnumerateWhileCreatingValue_Then_Lock()
	{
		var current = default(IDispatcher?);
		using var ui = new TestDispatcher("ui");
		var gate = new ManualResetEvent(false);

		var sut = new DispatcherLocal<string>(
			factory: d =>
			{
				if (d == ui)
				{
					gate.WaitOne();
				}

				return d is TestDispatcher td ? td.Name : "background";
			},
			schedulersProvider: () => current);

		// Begin to create value
		var getValue = Task.Run(() =>
		{
			current = ui;

			return sut.Value;
		});

		// Confirm that the creation is stuck
		await Task.Delay(100);
		getValue.IsCompleted.Should().BeFalse();

		// As we are creating a value, we cannot enumerate
		var enumeration = Task.Run(() => CountValues(sut));

		// Confirm that the enumeration and get value are stuck
		await Task.Delay(100);
		enumeration.IsCompleted.Should().BeFalse();
		getValue.IsCompleted.Should().BeFalse();

		// Release get value
		gate.Set();

		// Confirm both task completed ... and enumeration was effectively run only on first value
		for (var i = 0; i < 100 && (!enumeration.IsCompleted || !getValue.IsCompleted); i++)
		{
			await Task.Delay(10);
		}
		enumeration.IsCompleted.Should().BeTrue();
		getValue.IsCompleted.Should().BeTrue();

		enumeration.Result.Should().Be(1, because: "we should have got the value created for UI thread (on which we have waited on)");
	}

	private int CountValues<T>(DispatcherLocal<T> sut, bool includeBackground = true)
	{
		// note : This method MUST use ForEachValue for test When_EnumerateWhileCreatingValue_Then_Lock to be useful!
		var count = 0;
		sut.ForEachValue((_, __) => count++, includeBackground);
		return count;
	}
}
