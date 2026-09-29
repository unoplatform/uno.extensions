using Uno.Extensions.Storage.KeyValueStorage;

namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Carries the platform's default <see cref="IKeyValueStorage"/> to
/// <see cref="MsalAuthenticationProvider"/>.
/// </summary>
/// <remarks>
/// A wrapper rather than an <see cref="IKeyValueStorage"/> constructor parameter because a plain
/// <c>IKeyValueStorage</c> does not resolve to the platform default: <c>AddKeyedStorage</c>
/// registers <c>InMemoryKeyValueStorage</c> first and each provider registers the interface with
/// <c>TryAdd</c>, so the first one wins and injecting the interface silently yields in-memory
/// storage. The default instance is a *named* resolve (<c>GetRequiredDefaultInstance</c>), which
/// needs an <see cref="IServiceProvider"/> - hence a registration-time factory, the same shape
/// <c>UseAuthentication</c> uses to build <c>ITokenCache</c>.
/// </remarks>
/// <param name="Storage">The default key-value storage for the current platform.</param>
internal sealed record MsalKeyValueStorage(IKeyValueStorage Storage);
