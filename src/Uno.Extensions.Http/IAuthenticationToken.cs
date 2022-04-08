using System.Threading.Tasks;

namespace Uno.Extensions.Http;

public  interface IAuthenticationToken
{
	Task<string> GetAccessToken();
}
