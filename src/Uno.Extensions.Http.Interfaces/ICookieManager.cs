namespace Uno.Extensions;

/// <summary>
/// Manages request cookies
/// </summary>
internal interface ICookieManager
{
	/// <summary>
	/// Clears cookies for the specified request Uri
	/// </summary>
	/// <param name="Handler">The handler to access existing cookie container</param>
	/// <param name="requestUri">The request to clear cookies for</param>
	/// <returns>The (generated if not exists) cookie container</returns>
	CookieContainer? ClearCookies(HttpMessageHandler Handler, Uri requestUri);
}
