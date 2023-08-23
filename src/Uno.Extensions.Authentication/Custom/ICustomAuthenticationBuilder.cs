namespace Uno.Extensions.Authentication.Custom;

/// <summary>
/// Implemented by classes that are builders for the custom authentication provider feature.
/// </summary>
public interface ICustomAuthenticationBuilder : IBuilder
{
}

/// <summary>
/// Implemented by classes that are builders for the custom authentication provider feature.
/// </summary>
/// <typeparam name="TService">
/// A service type that is used by the custom authentication provider.
/// </typeparam>
public interface ICustomAuthenticationBuilder<TService> : IBuilder
{
}
