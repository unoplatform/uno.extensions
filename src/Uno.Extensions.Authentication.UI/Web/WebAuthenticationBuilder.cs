namespace Uno.Extensions.Authentication.Web;

internal record WebAuthenticationBuilder : BaseBuilder<WebAuthenticationSettings>, IWebAuthenticationBuilder
{
}

internal record WebAuthenticationBuilder<TService> : BaseBuilder<WebAuthenticationSettings<TService>>, IBuilder<WebAuthenticationSettings>, IWebAuthenticationBuilder<TService>
	where TService : notnull
{
	WebAuthenticationSettings IBuilder<WebAuthenticationSettings>.Settings { get => this.Settings; set => this.Settings = value as WebAuthenticationSettings<TService> ?? this.Settings; }
}
