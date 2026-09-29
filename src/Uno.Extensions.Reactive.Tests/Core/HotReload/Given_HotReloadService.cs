using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core.HotReload;

namespace Uno.Extensions.Reactive.Tests.Core.HotReload;

/// <summary>
/// Guards that <see cref="HotReloadService"/> does not retain hot-reloaded model types (and their
/// collectible <see cref="AssemblyLoadContext"/>s) forever.
/// </summary>
/// <remarks>
/// The shadow map is only ever added to by the metadata-update pipeline, so without release it keeps
/// every previewed app's model types — and their ALC — alive for the process lifetime in a downstream
/// host that loads previewed apps into their own collectible ALCs.
/// </remarks>
[TestClass]
public class Given_HotReloadService
{
	[TestCleanup]
	public void Cleanup() => HotReloadService.Reset();

	[TestMethod]
	public void When_Reset_Then_ShadowMapIsCleared()
	{
		HotReloadService.TrackShadowForTest(typeof(OriginalStub), typeof(ShadowStub));
		HotReloadService.TrackedShadowCount.Should().BeGreaterThan(0);

		HotReloadService.Reset();

		HotReloadService.TrackedShadowCount.Should().Be(0);
	}

	[TestMethod]
	public void When_ClearCache_Then_OnlyMatchingEntriesRemoved()
	{
		HotReloadService.TrackShadowForTest(typeof(OriginalStub), typeof(ShadowStub));
		HotReloadService.TrackShadowForTest(typeof(OtherOriginalStub), typeof(OtherShadowStub));
		HotReloadService.TrackedShadowCount.Should().Be(2);

		// Clearing by the shadow value must drop the whole entry.
		HotReloadService.ClearCache(new[] { typeof(ShadowStub) });

		HotReloadService.TrackedShadowCount.Should().Be(1);
	}

	[TestMethod]
	public void When_TrackedTypeFromCollectibleAlc_Then_AlcIsCollectibleAfterUnload()
	{
		var alcReference = TrackTypeFromCollectibleAlc();

		for (var i = 0; i < 10 && alcReference.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		alcReference.IsAlive.Should().BeFalse(
			"HotReloadService must not keep a previewed app's collectible ALC alive via the shadow map");
	}

	// Separate non-inlined frame so the ALC + type locals do not linger on the caller's stack.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference TrackTypeFromCollectibleAlc()
	{
		// Load an on-disk assembly into a fresh collectible ALC. This produces a distinct Type identity
		// scoped to that ALC — the shape a downstream host sees for a previewed app's model type.
		var probePath = typeof(HotReloadService).Assembly.Location;

		var alc = new AssemblyLoadContext(nameof(Given_HotReloadService), isCollectible: true);
		var assembly = alc.LoadFromAssemblyPath(probePath);
		var appType = assembly.GetType(typeof(HotReloadService).FullName!, throwOnError: true)!;

		// Sanity: the type must belong to the collectible ALC, not the default one.
		AssemblyLoadContext.GetLoadContext(appType.Assembly).Should().BeSameAs(alc);

		// Track a mapping keyed by a type from the collectible ALC.
		HotReloadService.TrackShadowForTest(appType, appType);

		var reference = new WeakReference(alc, trackResurrection: false);

		// The host tears the app down: unload the ALC. The service's unload handler must drop the entry.
		appType = null;
		assembly = null;
		alc.Unload();
		return reference;
	}

	private sealed class OriginalStub { }
	private sealed class ShadowStub { }
	private sealed class OtherOriginalStub { }
	private sealed class OtherShadowStub { }
}
