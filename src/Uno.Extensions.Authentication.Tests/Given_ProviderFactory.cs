using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication;

[TestClass]
public class Given_ProviderFactory
{
	[TestMethod]
	public async Task When_ResolvedConcurrently_Then_ConfiguredOnceAndSameInstance()
	{
		// ConfigureProvider is where MSAL builds its client application and subscribes to
		// ITokenCache.Cleared; a bare `??=` let concurrent resolves run it more than once, with the
		// losing copies left subscribed and unreachable.
		var configured = 0;
		var factory = new ProviderFactory<FakeAuthenticationProvider, object>(
			"Fake",
			new FakeAuthenticationProvider(),
			new object(),
			(provider, _) =>
			{
				Interlocked.Increment(ref configured);
				return new FakeAuthenticationProvider(provider.Name);
			});

		var resolved = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => factory.AuthenticationProvider)));

		configured.Should().Be(1);
		resolved.Distinct().Should().ContainSingle("every caller must get the one configured instance");
	}
}
