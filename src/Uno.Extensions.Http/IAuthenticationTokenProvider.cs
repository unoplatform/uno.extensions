using System.Threading.Tasks;

namespace Uno.Extensions.Http;

public  interface IAuthenticationTokenProvider
{
	Task<string> GetAccessToken();
}
