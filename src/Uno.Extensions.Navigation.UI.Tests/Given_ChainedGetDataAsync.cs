using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for chained GetDataAsync navigation scenarios.
/// Replicates the drivernav sample app route structure where multiple pages
/// return the same result type (ResultEntity) and GetDataAsync calls can be chained
/// (e.g., PageA → GetDataAsync → PageB → GetDataAsync → PageC → back with result → PageB → back with result → PageA).
///
/// The bug: in chained GetDataAsync scenarios, the first awaiter's GetDataAsync would hang forever
/// when returning data back through the chain, because the DI navigator (ResponseNavigator)
/// was being overwritten during back navigation.
///
/// Route structure (mirrors drivernav):
///   "" (root)
///     ├── "Main" (MainPage) [IsDefault]
///     ├── "Sibling" (SiblingPage) — ViewMap with ResultData = ResultEntity
///     ├── "SiblingTwo" (SiblingTwoPage) — ViewMap with ResultData = ResultEntity
///     └── "Second" (SecondPage) — ViewMap with ResultData = ResultEntity
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ChainedGetDataAsync
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Builds a navigation host with the drivernav route structure:
	/// Main page + three sibling pages that all return ResultEntity via ResultData.
	/// </summary>
	private async Task<(IHost Host, INavigator Navigator, ContentControl Root)> SetupNavigationAsync()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_ChainedGetDataAsync).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<MainPage>(),
								new ViewMap<SiblingPage>(ResultData: typeof(ResultEntity)),
								new ViewMap<SiblingTwoPage>(ResultData: typeof(ResultEntity)),
								new ViewMap<SecondPage>(ResultData: typeof(ResultEntity)));

							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("Main", View: views.FindByView<MainPage>(), IsDefault: true),
									new RouteMap("Sibling", View: views.FindByView<SiblingPage>()),
									new RouteMap("SiblingTwo", View: views.FindByView<SiblingTwoPage>()),
									new RouteMap("Second", View: views.FindByView<SecondPage>()),
								}));
						})
					.Build();
				return h;
			},
			initialRoute: "Main");

		var root = (ContentControl)window.Content!;
		var navigator = root.Navigator()!;

		return (host, navigator, root);
	}

	/// <summary>
	/// Single-level GetDataAsync: Navigate forward for result, return result, original call completes.
	///
	/// Flow: Main → GetDataAsync → Sibling → NavigateBackWithResultAsync → Main receives result
	///
	/// This is the basic scenario that should always work. If this fails, the navigation
	/// result mechanism is fundamentally broken.
	/// </summary>
	[TestMethod]
	public async Task When_SingleLevel_GetDataAsync_Then_ResultIsReturned()
	{
		// Arrange
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Act — Start GetDataAsync (navigates forward to Sibling, then suspends awaiting result)
			var getDataTask = navigator.GetDataAsync<ResultEntity>(root);

			// The forward navigation should have completed synchronously on the UI thread.
			// Now the Frame shows SiblingPage and a ResponseNavigator is in DI.
			// Wait briefly for navigation to settle.
			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts.Token);

			// Get the navigator that SiblingPage's ViewModel would receive from DI
			var siblingNav = root.Navigator()!;
			siblingNav.Should().BeAssignableTo<IResponseNavigator>(
				"after GetDataAsync forward nav, the DI navigator should be a ResponseNavigator");

			// Simulate SiblingModel.ReturnData() — navigate back with result
			await siblingNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from Sibling")));

			// Assert — The GetDataAsync task should now complete with the returned data
			using var resultCts = new CancellationTokenSource(Timeout);
			var result = await getDataTask.WaitAsync(resultCts.Token);

			result.Should().NotBeNull("GetDataAsync should return the entity passed via NavigateBackWithResultAsync");
			result!.Name.Should().Be("Data from Sibling");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Chained (two-level) GetDataAsync: The core scenario that was broken.
	///
	/// Flow:
	///   Main → GetDataAsync → Sibling → GetDataAsync → SiblingTwo
	///     → NavigateBackWithResultAsync → Sibling (receives SiblingTwo's result)
	///     → NavigateBackWithResultAsync → Main (receives Sibling's result)
	///
	/// The bug was that Main's GetDataAsync would hang forever because the ResponseNavigator
	/// for the outer chain was being overwritten in DI during back navigation from SiblingTwo.
	/// </summary>
	[TestMethod]
	public async Task When_ChainedTwoLevel_GetDataAsync_Then_BothResultsAreReturned()
	{
		// Arrange
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Step 1: Main → GetDataAsync → navigates to Sibling
			var outerGetDataTask = navigator.GetDataAsync<ResultEntity>(root);

			using var navCts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts1.Token);

			// Capture the navigator that SiblingPage's ViewModel would use (ResponseNavigator-A)
			var siblingNav = root.Navigator()!;
			siblingNav.Should().BeAssignableTo<IResponseNavigator>(
				"after first GetDataAsync, DI should have ResponseNavigator-A");

			// Step 2: Sibling → GetDataAsync → navigates to SiblingTwo
			// This creates ResponseNavigator-B wrapping the frame navigator
			var innerGetDataTask = siblingNav.GetDataAsync<ResultEntity>(root);

			using var navCts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					// The navigator should be a different ResponseNavigator (B, not A)
					return nav is IResponseNavigator && !ReferenceEquals(nav, siblingNav);
				},
				navCts2.Token);

			// This is ResponseNavigator-B (what SiblingTwoPage's ViewModel would get)
			var siblingTwoNav = root.Navigator()!;
			siblingTwoNav.Should().BeAssignableTo<IResponseNavigator>(
				"after second GetDataAsync, DI should have ResponseNavigator-B");
			siblingTwoNav.Should().NotBeSameAs(siblingNav,
				"ResponseNavigator-B should be a different instance from ResponseNavigator-A");

			// Step 3: SiblingTwo → NavigateBackWithResultAsync → returns to Sibling
			await siblingTwoNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from SiblingTwo")));

			// The inner GetDataAsync should now complete
			using var innerCts = new CancellationTokenSource(Timeout);
			var innerResult = await innerGetDataTask.WaitAsync(innerCts.Token);

			innerResult.Should().NotBeNull("Inner GetDataAsync should complete with SiblingTwo's data");
			innerResult!.Name.Should().Be("Data from SiblingTwo");

			// Step 4: After back nav, the fix should have restored ResponseNavigator-A in DI
			using var navCts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts3.Token);

			var restoredNav = root.Navigator()!;
			restoredNav.Should().BeAssignableTo<IResponseNavigator>(
				"after back nav from SiblingTwo, the outer ResponseNavigator-A should be restored in DI");

			// Step 5: Sibling → NavigateBackWithResultAsync → returns to Main
			await restoredNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from Sibling")));

			// The outer GetDataAsync should now complete
			using var outerCts = new CancellationTokenSource(Timeout);
			var outerResult = await outerGetDataTask.WaitAsync(outerCts.Token);

			outerResult.Should().NotBeNull("Outer GetDataAsync should complete with Sibling's data");
			outerResult!.Name.Should().Be("Data from Sibling");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Three-level chained GetDataAsync: Exercises the deepest chain.
	///
	/// Flow:
	///   Main → GetDataAsync → Sibling → GetDataAsync → SiblingTwo → GetDataAsync → Second
	///     → NavigateBackWithResultAsync → SiblingTwo (receives Second's result)
	///     → NavigateBackWithResultAsync → Sibling (receives SiblingTwo's result)
	///     → NavigateBackWithResultAsync → Main (receives Sibling's result)
	///
	/// Each level returns its own data, and each GetDataAsync in the chain must complete
	/// with the correct result.
	/// </summary>
	[TestMethod]
	public async Task When_ChainedThreeLevel_GetDataAsync_Then_AllResultsAreReturned()
	{
		// Arrange
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Level 1: Main → GetDataAsync → Sibling
			var level1Task = navigator.GetDataAsync<ResultEntity>(root);

			using var navCts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts1.Token);
			var navOnSibling = root.Navigator()!;

			// Level 2: Sibling → GetDataAsync → SiblingTwo
			var level2Task = navOnSibling.GetDataAsync<ResultEntity>(root);

			using var navCts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					return nav is IResponseNavigator && !ReferenceEquals(nav, navOnSibling);
				},
				navCts2.Token);
			var navOnSiblingTwo = root.Navigator()!;

			// Level 3: SiblingTwo → GetDataAsync → Second
			var level3Task = navOnSiblingTwo.GetDataAsync<ResultEntity>(root);

			using var navCts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					return nav is IResponseNavigator && !ReferenceEquals(nav, navOnSiblingTwo);
				},
				navCts3.Token);
			var navOnSecond = root.Navigator()!;

			// Unwind: Second → back to SiblingTwo
			await navOnSecond.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from Second")));

			using var level3Cts = new CancellationTokenSource(Timeout);
			var level3Result = await level3Task.WaitAsync(level3Cts.Token);
			level3Result.Should().NotBeNull();
			level3Result!.Name.Should().Be("Data from Second");

			// Unwind: SiblingTwo → back to Sibling
			using var navCts4 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts4.Token);
			var restoredNavSiblingTwo = root.Navigator()!;

			await restoredNavSiblingTwo.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from SiblingTwo")));

			using var level2Cts = new CancellationTokenSource(Timeout);
			var level2Result = await level2Task.WaitAsync(level2Cts.Token);
			level2Result.Should().NotBeNull();
			level2Result!.Name.Should().Be("Data from SiblingTwo");

			// Unwind: Sibling → back to Main
			using var navCts5 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts5.Token);
			var restoredNavSibling = root.Navigator()!;

			await restoredNavSibling.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Data from Sibling")));

			using var level1Cts = new CancellationTokenSource(Timeout);
			var level1Result = await level1Task.WaitAsync(level1Cts.Token);
			level1Result.Should().NotBeNull();
			level1Result!.Name.Should().Be("Data from Sibling");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// After a successful chained GetDataAsync round-trip, the navigator should be
	/// restored to the original (non-ResponseNavigator) state, allowing further navigation.
	/// </summary>
	[TestMethod]
	public async Task When_ChainedGetDataAsync_Completes_Then_NavigatorIsRestoredToOriginal()
	{
		// Arrange
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Do a single-level GetDataAsync round-trip
			var getDataTask = navigator.GetDataAsync<ResultEntity>(root);

			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts.Token);

			var siblingNav = root.Navigator()!;
			await siblingNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("First trip")));

			using var resultCts = new CancellationTokenSource(Timeout);
			var result = await getDataTask.WaitAsync(resultCts.Token);
			result.Should().NotBeNull();

			// Assert — navigator should no longer be a ResponseNavigator
			using var restoreCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is not IResponseNavigator,
				restoreCts.Token);

			var restoredNav = root.Navigator()!;
			restoredNav.Should().NotBeAssignableTo<IResponseNavigator>(
				"after GetDataAsync completes, the navigator should be restored to the original (non-Response) navigator");

			// Verify we can do another GetDataAsync round-trip
			var secondGetDataTask = restoredNav.GetDataAsync<ResultEntity>(root);

			using var navCts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts2.Token);

			var secondSiblingNav = root.Navigator()!;
			await secondSiblingNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Second trip")));

			using var resultCts2 = new CancellationTokenSource(Timeout);
			var secondResult = await secondGetDataTask.WaitAsync(resultCts2.Token);
			secondResult.Should().NotBeNull();
			secondResult!.Name.Should().Be("Second trip");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Two-level chained GetDataAsync followed by a second independent GetDataAsync.
	/// Ensures the navigator is fully restored after a chain completes, allowing
	/// further navigate-for-result operations.
	/// </summary>
	[TestMethod]
	public async Task When_ChainedGetDataAsync_Completes_Then_SecondChainAlsoWorks()
	{
		// Arrange
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// --- First chain: Main → Sibling → SiblingTwo → back → back ---
			var outerTask1 = navigator.GetDataAsync<ResultEntity>(root);

			using var cts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				cts1.Token);

			var nav1 = root.Navigator()!;
			var innerTask1 = nav1.GetDataAsync<ResultEntity>(root);

			using var cts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var n = root.Navigator();
					return n is IResponseNavigator && !ReferenceEquals(n, nav1);
				},
				cts2.Token);

			var nav2 = root.Navigator()!;
			// SiblingTwo → back with result
			await nav2.NavigateBackWithResultAsync(root, data: Option.Some(new ResultEntity("Chain1-Inner")));

			using var innerCts1 = new CancellationTokenSource(Timeout);
			var innerResult1 = await innerTask1.WaitAsync(innerCts1.Token);
			innerResult1.Should().NotBeNull();

			// Sibling → back with result
			using var cts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() => root.Navigator() is IResponseNavigator, cts3.Token);
			var nav3 = root.Navigator()!;
			await nav3.NavigateBackWithResultAsync(root, data: Option.Some(new ResultEntity("Chain1-Outer")));

			using var outerCts1 = new CancellationTokenSource(Timeout);
			var outerResult1 = await outerTask1.WaitAsync(outerCts1.Token);
			outerResult1.Should().NotBeNull();
			outerResult1!.Name.Should().Be("Chain1-Outer");

			// --- Second chain: should work identically ---
			using var cts4 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() => root.Navigator() is not IResponseNavigator, cts4.Token);
			var freshNav = root.Navigator()!;

			var outerTask2 = freshNav.GetDataAsync<ResultEntity>(root);

			using var cts5 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() => root.Navigator() is IResponseNavigator, cts5.Token);

			var nav4 = root.Navigator()!;
			await nav4.NavigateBackWithResultAsync(root, data: Option.Some(new ResultEntity("Chain2-Result")));

			using var outerCts2 = new CancellationTokenSource(Timeout);
			var outerResult2 = await outerTask2.WaitAsync(outerCts2.Token);
			outerResult2.Should().NotBeNull();
			outerResult2!.Name.Should().Be("Chain2-Result");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// When GetDataAsync is cancelled via CancellationToken before a result is returned,
	/// the ResponseNavigator should apply a None result and the awaiter should receive null.
	///
	/// The ResponseNavigator constructor registers a cancellation callback that calls
	/// ApplyResult(Option.None&lt;TResult&gt;()), which should:
	/// 1. Complete the TaskCompletionSource with None
	/// 2. Restore the original navigator in DI
	/// </summary>
	[TestMethod]
	public async Task When_GetDataAsync_IsCancelled_Then_ReturnsNullAndRestoresNavigator()
	{
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Create a CancellationTokenSource that we control
			using var userCts = new CancellationTokenSource();

			// Start GetDataAsync with our cancellable token
			var getDataTask = navigator.GetDataAsync<ResultEntity>(root, cancellation: userCts.Token);

			// Wait for the ResponseNavigator to be set up in DI
			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts.Token);

			root.Navigator().Should().BeAssignableTo<IResponseNavigator>(
				"after GetDataAsync forward nav, the DI navigator should be a ResponseNavigator");

			// Cancel the token — this should trigger the cancellation callback in ResponseNavigator
			userCts.Cancel();

			// The GetDataAsync task should now complete (with null / default since None)
			using var resultCts = new CancellationTokenSource(Timeout);
			var result = await getDataTask.WaitAsync(resultCts.Token);
			result.Should().BeNull("GetDataAsync should return null/default when cancelled");

			// After cancellation + result applied, the navigator should be restored
			using var restoreCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is not IResponseNavigator,
				restoreCts.Token);

			root.Navigator().Should().NotBeAssignableTo<IResponseNavigator>(
				"after cancellation, the navigator should be restored to the original (non-Response) navigator");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// When the user manually navigates to a different route (not back) while GetDataAsync
	/// is pending, the ResponseNavigator in DI gets replaced by the plain navigator
	/// (see Navigator.RegionNavigateAsync line ~651 where responseNavigator is null → services.AddScopedInstance).
	///
	/// The GetDataAsync should eventually time out or remain pending (the TaskCompletionSource
	/// is never completed). The key thing to verify is that the navigation infrastructure
	/// doesn't crash and subsequent navigation still works.
	/// </summary>
	[TestMethod]
	public async Task When_UserNavigatesAway_DuringGetDataAsync_Then_NavigatorIsReplaced()
	{
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Start GetDataAsync which navigates to Sibling
			var getDataTask = navigator.GetDataAsync<ResultEntity>(root);

			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts.Token);

			var responseNav = root.Navigator()!;
			responseNav.Should().BeAssignableTo<IResponseNavigator>(
				"after GetDataAsync, DI should have ResponseNavigator");

			// User manually navigates to a *different* route (not back with result)
			// This simulates clicking a navigation link instead of using the expected back-with-result flow.
			// The Navigator.RegionNavigateAsync will replace the DI navigator with itself (non-response).
			await responseNav.NavigateRouteAsync(root, route: "SiblingTwo");

			// Wait for navigation to settle
			using var navCts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					// After forward nav without Result, the DI navigator should no longer be the original ResponseNavigator
					return nav is not null && !ReferenceEquals(nav, responseNav);
				},
				navCts2.Token);

			// The navigator should have been replaced (may or may not be a ResponseNavigator depending
			// on implementation details, but it should not be the original ResponseNavigator)
			root.Navigator().Should().NotBeSameAs(responseNav,
				"after manual forward navigation, the DI navigator should be replaced");

			// The original GetDataAsync task is now orphaned — its TaskCompletionSource will never
			// be completed by the normal flow. Verify it hasn't faulted.
			getDataTask.IsFaulted.Should().BeFalse(
				"the orphaned GetDataAsync should not fault");

			// Verify subsequent navigation still works (navigate back to a known state)
			var currentNav = root.Navigator()!;
			await currentNav.NavigateBackAsync(root);

			using var backCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is not null,
				backCts.Token);
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// When GetDataAsync is cancelled in a chained scenario (outer pending, inner cancelled),
	/// the inner cancellation should not break the outer chain's ability to receive its result.
	///
	/// Flow:
	///   Main → GetDataAsync → Sibling → GetDataAsync(cancelled) → SiblingTwo
	///   Inner GetDataAsync is cancelled while on SiblingTwo.
	///   Then the user navigates back from SiblingTwo to Sibling (without result).
	///   Then Sibling returns a result to Main via NavigateBackWithResultAsync.
	///   Main's outer GetDataAsync should still complete successfully.
	/// </summary>
	[TestMethod]
	public async Task When_InnerGetDataAsync_IsCancelled_Then_OuterChainStillWorks()
	{
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Step 1: Main → GetDataAsync → Sibling (outer)
			var outerTask = navigator.GetDataAsync<ResultEntity>(root);

			using var cts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				cts1.Token);
			var siblingNav = root.Navigator()!;

			// Step 2: Sibling → GetDataAsync(cancellable) → SiblingTwo (inner)
			using var innerCts = new CancellationTokenSource();
			var innerTask = siblingNav.GetDataAsync<ResultEntity>(root, cancellation: innerCts.Token);

			using var cts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					return nav is IResponseNavigator && !ReferenceEquals(nav, siblingNav);
				},
				cts2.Token);

			// Step 3: Cancel the inner GetDataAsync
			innerCts.Cancel();

			// Inner task should complete with null (None)
			using var innerResultCts = new CancellationTokenSource(Timeout);
			var innerResult = await innerTask.WaitAsync(innerResultCts.Token);
			innerResult.Should().BeNull("cancelled inner GetDataAsync should return null");

			// Step 4: Navigate back from SiblingTwo to Sibling (plain back, no result)
			var currentNav = root.Navigator()!;
			await currentNav.NavigateBackAsync(root);

			// Wait for back navigation to settle and the outer ResponseNavigator to be restored
			using var cts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				cts3.Token);

			// Step 5: Sibling returns result to Main (outer chain)
			var restoredNav = root.Navigator()!;
			await restoredNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Outer result after inner cancel")));

			// Outer task should complete successfully
			using var outerResultCts = new CancellationTokenSource(Timeout);
			var outerResult = await outerTask.WaitAsync(outerResultCts.Token);
			outerResult.Should().NotBeNull("outer GetDataAsync should complete even though inner was cancelled");
			outerResult!.Name.Should().Be("Outer result after inner cancel");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// When the user navigates back without providing a result (plain back navigation
	/// instead of NavigateBackWithResultAsync), the ResponseNavigator should detect the
	/// back navigation and apply a None result, completing the GetDataAsync with null.
	/// </summary>
	[TestMethod]
	public async Task When_NavigateBack_WithoutResult_Then_GetDataAsyncReturnsNull()
	{
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Start GetDataAsync → navigates to Sibling
			var getDataTask = navigator.GetDataAsync<ResultEntity>(root);

			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				navCts.Token);

			var siblingNav = root.Navigator()!;

			// Navigate back WITHOUT a result (simulates user pressing back button)
			await siblingNav.NavigateBackAsync(root);

			// The GetDataAsync should complete with null (None result)
			using var resultCts = new CancellationTokenSource(Timeout);
			var result = await getDataTask.WaitAsync(resultCts.Token);
			result.Should().BeNull(
				"GetDataAsync should return null when user navigates back without providing a result");

			// Navigator should be restored
			using var restoreCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is not IResponseNavigator,
				restoreCts.Token);

			root.Navigator().Should().NotBeAssignableTo<IResponseNavigator>(
				"after back-without-result, the navigator should be restored");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// In a two-level chain, when the deepest page navigates back without a result
	/// (e.g. user presses back button on SiblingTwo), the middle page's inner GetDataAsync
	/// should complete with null, but the outer chain should remain intact — the middle
	/// page can still return a result to the outer caller.
	/// </summary>
	[TestMethod]
	public async Task When_ChainedInnerNavigatesBack_WithoutResult_Then_OuterChainSurvives()
	{
		var (host, navigator, root) = await SetupNavigationAsync();

		try
		{
			// Outer: Main → GetDataAsync → Sibling
			var outerTask = navigator.GetDataAsync<ResultEntity>(root);

			using var cts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				cts1.Token);
			var siblingNav = root.Navigator()!;

			// Inner: Sibling → GetDataAsync → SiblingTwo
			var innerTask = siblingNav.GetDataAsync<ResultEntity>(root);

			using var cts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() =>
				{
					var nav = root.Navigator();
					return nav is IResponseNavigator && !ReferenceEquals(nav, siblingNav);
				},
				cts2.Token);

			var siblingTwoNav = root.Navigator()!;

			// SiblingTwo navigates back WITHOUT result (user presses back)
			await siblingTwoNav.NavigateBackAsync(root);

			// Inner task should complete with null
			using var innerCts = new CancellationTokenSource(Timeout);
			var innerResult = await innerTask.WaitAsync(innerCts.Token);
			innerResult.Should().BeNull(
				"inner GetDataAsync should return null when the target navigates back without result");

			// Outer chain should still be active: the outer ResponseNavigator should be restored
			using var cts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => root.Navigator() is IResponseNavigator,
				cts3.Token);

			// Sibling can still return a result to Main
			var restoredNav = root.Navigator()!;
			await restoredNav.NavigateBackWithResultAsync(
				root,
				data: Option.Some(new ResultEntity("Outer result")));

			using var outerCts = new CancellationTokenSource(Timeout);
			var outerResult = await outerTask.WaitAsync(outerCts.Token);
			outerResult.Should().NotBeNull(
				"outer GetDataAsync should complete successfully even though inner returned no result");
			outerResult!.Name.Should().Be("Outer result");
		}
		finally
		{
			await host.StopAsync();
		}
	}
}
