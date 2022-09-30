using System.Net;

namespace Uno.Extensions;

internal interface ICookieManager
{
	CookieContainer? ClearCookies(HttpMessageHandler Handler, Uri requestUri);
}
