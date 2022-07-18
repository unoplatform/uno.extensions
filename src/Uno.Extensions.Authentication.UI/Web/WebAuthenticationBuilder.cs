namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationBuilder : BaseBuilder<WebAuthenticationSettings>, IWebAuthenticationBuilder
{
}

internal record WebAuthenticationBuilder<TService> : BaseBuilder<WebAuthenticationSettings<TService>>, IWebAuthenticationBuilder<TService>
	where TService : notnull
{
}
