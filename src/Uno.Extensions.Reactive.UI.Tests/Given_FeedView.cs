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

	#region Mock (non-feed) sources
	private record MockValue(string Name);

	private record MockEnvelope
	{
		public object? Data { get; init; }
		public bool Progress { get; init; }
		public object? Error { get; init; }
	}

	[TestMethod]
	public async Task When_SourceIsPoco_Then_RendersValue()
	{
		var value = new MockValue("42");
		var sut = new FeedView { Source = value };

		await UIHelper.Load(sut, CT);

		await TestHelper.WaitFor(() => ReferenceEquals(sut.State.Data, value), CT);
		(sut as ILoadable).IsExecuting.Should().BeFalse("a mocked value is not loading");
	}

	[TestMethod]
	public async Task When_SourceIsEnvelopeWithData_Then_RendersDataValue()
	{
		var value = new MockValue("42");
		var sut = new FeedView { Source = new MockEnvelope { Data = value } };

		await UIHelper.Load(sut, CT);

		await TestHelper.WaitFor(() => ReferenceEquals(sut.State.Data, value), CT);
	}

	[TestMethod]
	public async Task When_SourceIsEnvelopeWithProgress_Then_StaysLoading()
	{
		var sut = new FeedView { Source = new MockEnvelope { Progress = true } };

		await UIHelper.Load(sut, CT);

		await TestHelper.WaitFor(() => sut.State.Progress, CT);
		(sut as ILoadable).IsExecuting.Should().BeTrue("a progress mock models an in-flight load");
	}

	[TestMethod]
	public async Task When_SourceIsEnvelopeWithError_Then_ExposesError()
	{
		var error = new InvalidOperationException("mocked failure");
		var sut = new FeedView { Source = new MockEnvelope { Error = error } };

		await UIHelper.Load(sut, CT);

		await TestHelper.WaitFor(() => sut.State.Error == error, CT);
	}

	[TestMethod]
	public async Task When_SourceIsEnvelopeWithNullData_Then_None()
	{
		var sut = new FeedView { Source = new MockEnvelope { Data = null } };
		var sutAsLoadable = sut as ILoadable;

		await UIHelper.Load(sut, CT);

		await TestHelper.WaitFor(() => !sutAsLoadable.IsExecuting, CT);
		sut.State.Data.Should().BeNull("a null Data member mocks the None state");
		sut.State.Error.Should().BeNull();
	}

	[TestMethod]
	public async Task When_MockSourceAndReloaded_Then_StillRenders()
	{
		var value = new MockValue("42");
		var sut = new FeedView { Source = value };
		var root = new Grid { Children = { sut } };

		await UIHelper.Load(root, CT);
		await TestHelper.WaitFor(() => ReferenceEquals(sut.State.Data, value), CT);

		// Unload and reload the view: Enable must re-subscribe using the cached coerced source.
		root.Children.Remove(sut);
		await TestHelper.WaitFor(() => !sut.IsLoaded, CT);
		root.Children.Add(sut);

		await TestHelper.WaitFor(() => ReferenceEquals(sut.State.Data, value), CT);
		(sut as ILoadable).IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_MockSourceAndRefresh_Then_RefreshCompletes()
	{
		var value = new MockValue("42");
		var sut = new FeedView { Source = new MockEnvelope { Data = value } };

		await UIHelper.Load(sut, CT);
		await TestHelper.WaitFor(() => ReferenceEquals(sut.State.Data, value), CT);

		sut.Refresh.Execute(null);

		await TestHelper.WaitFor(() => !sut.Refresh.IsExecuting, CT);
		sut.State.Data.Should().Be(value, "refreshing a mock re-emits the mocked value");
	}
	#endregion
}
