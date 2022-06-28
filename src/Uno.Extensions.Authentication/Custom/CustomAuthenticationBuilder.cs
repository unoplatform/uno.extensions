namespace Uno.Extensions.Authentication.Custom;

internal record CustomAuthenticationBuilder : BaseBuilder<CustomAuthenticationSettings>, ICustomAuthenticationBuilder
{
}


internal record CustomAuthenticationBuilder<TService> : BaseBuilder<CustomAuthenticationSettings<TService>>, ICustomAuthenticationBuilder<TService>
{
}
