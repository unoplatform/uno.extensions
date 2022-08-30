using System.Net;

namespace Uno.Extensions;

public interface ICookieManager
{
	void ClearCookies(HttpMessageHandler Handler, HttpRequestMessage message);
}
