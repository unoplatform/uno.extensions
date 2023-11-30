using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.UI;
using Uno.Toolkit;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_FeedView : FeedTests
{
	[TestMethod]
	public async Task When_Loading()
	{
		var tcs = new TaskCompletionSource<int>();
		var src = Feed.Async(async ct => await tcs.Task);
		var sut = new FeedView { Source = src };
		var sutAsLoadable = sut as ILoadable;

		sutAsLoadable.IsExecuting.Should().BeTrue("The FeedView should consider itself as loading even before being inserted in the visual tree.");

		var isLoadingValues = new List<bool>();
		sutAsLoadable.IsExecutingChanged += (snd, e) => isLoadingValues.Add(sutAsLoadable.IsExecuting);

		await UIHelper.Load(sut, CT);

		isLoadingValues.Should().BeEmpty("The IsLoading should not have changed yet");

		tcs.SetResult(42);

		await TestHelper.WaitFor(() => isLoadingValues.Count > 0, CT);
	}

	[TestMethod]
	public async Task When_NotVisible_Then_DoesNotSubscribeToSource()
	{
		// This tests will also ensure that is the FeedView will not try to GoToState while it does not have any template yet.

		var isLoaded = false;
		var src = Feed.Async(async ct => isLoaded = true);
		var sut = new FeedView { Source = src };
		var root = new Grid { Visibility = Visibility.Collapsed, Children = { sut } };

		await UIHelper.Load(root, CT);

		isLoaded.Should().BeFalse("The FeedView should not have subscribed to the source while it is not visible.");

		root.Visibility = Visibility.Visible;

		await TestHelper.WaitFor(() => isLoaded, CT);

		isLoaded.Should().BeTrue("The FeedView should have subscribed to the source when it became visible.");
	}


	[TestMethod]
	public async Task When_GetSource_Then_ContextContainsDispatcher()
	{
		var result = new TaskCompletionSource<bool>();
		var timeout = new CancellationTokenSource(UIHelper.DefaultTimeout).Token;
		using var _ = CancellationTokenSource.CreateLinkedTokenSource(CT, timeout).Token.Register(() => result.TrySetResult(false));
		var src = Feed.Async(async ct => result.TrySetResult(SourceContext.Current.FindDispatcher() is not null));
		var sut = new FeedView { Source = src };

		await UIHelper.Load(sut, CT);

		(await result.Task).Should().BeTrue();
	}
}
