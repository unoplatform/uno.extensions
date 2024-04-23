using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

[TestClass]
public partial class Given_BindableViewModelBase : FeedUITests
{
	[TestMethod]
	public async Task When_UpdateSourceMultipleTimeWhileUIThreadFreeze_Then_LastWin()
	{
		var sut = new BindableWhen_UpdateSourceMultipleTimeWhileUIThreadFreeze_Then_LastWin_Model();

		var changes = 0;
		var uiFrozen = new ManualResetEvent(false);
		var bgCompleted = new ManualResetEvent(false);

		// First complete init, including changes raised on the UI thread
		await Dispatcher.ExecuteAsync(_ => sut.PropertyChanged += (snd, e) => changes++, CT);
		await WaitFor(() => sut.Value == 42 && changes is 1);

		// Freeze the UI thread
		Dispatcher.TryEnqueue(() =>
		{
			uiFrozen.Set();
			bgCompleted.WaitOne();
		}).Should().BeTrue();
		uiFrozen.WaitOne(1000).Should().BeTrue();

		// Update the source multiple times from bg thread
		await sut.Model.Value.SetAsync(43, CT);
		await sut.Model.Value.SetAsync(44, CT);
		await sut.Model.Value.SetAsync(45, CT);
		await sut.Model.Value.SetAsync(46, CT);

		// Release the UI thread so it can process the changes
		bgCompleted.Set();
		await Dispatcher.ExecuteAsync(_ => { }, CT); // Wait for the UI thread to run something

		// Confirm the last change has been applied
		await WaitFor(() => sut.Value == 46);
		changes.Should().Be(2); // And only one property changed should have been raised for all changes
	}

	public partial class When_UpdateSourceMultipleTimeWhileUIThreadFreeze_Then_LastWin_Model
	{
		public IState<int> Value => State<int>.Value(this, () => 42);
	}

	private async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < 100; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(1);
		}

		throw new TimeoutException();
	}
}
