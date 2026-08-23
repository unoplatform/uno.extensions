using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Scripted <see cref="IAuthenticationProvider"/>: each call answers with whatever the test set up.
/// </summary>
internal sealed class FakeAuthenticationProvider : IAuthenticationProvider
{
	public FakeAuthenticationProvider(string name = "Fake") => Name = name;

	public string Name { get; }

	/// <summary>What <see cref="RefreshAsync"/> returns; <c>null</c> means "could not refresh".</summary>
	public Func<IDictionary<string, string>?> OnRefresh { get; set; } = () => null;

	public int RefreshCount { get; private set; }

	public ValueTask<IDictionary<string, string>?> LoginAsync(IDispatcher? dispatcher, IDictionary<string, string>? credentials, CancellationToken cancellationToken) =>
		new(credentials);

	public ValueTask<bool> LogoutAsync(IDispatcher? dispatcher, CancellationToken cancellationToken) =>
		new(true);

	public ValueTask<IDictionary<string, string>?> RefreshAsync(CancellationToken cancellationToken)
	{
		RefreshCount++;
		return new(OnRefresh());
	}
}

/// <summary>
/// <see cref="IProviderFactory"/> that counts how many times its provider is read, so a test can
/// prove the service builds its provider table exactly once.
/// </summary>
internal sealed class CountingProviderFactory : IProviderFactory
{
	private readonly IAuthenticationProvider _provider;
	private int _reads;

	public CountingProviderFactory(IAuthenticationProvider provider) => _provider = provider;

	public string Name => _provider.Name;

	public int Reads => _reads;

	public IAuthenticationProvider AuthenticationProvider
	{
		get
		{
			Interlocked.Increment(ref _reads);
			return _provider;
		}
	}
}
