using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions.Http.Handlers;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class TestToken : IAuthenticationToken
	{
		public TestToken(string accessToken, string refreshToken = null)
		{
			AccessToken = accessToken;
			RefreshToken = refreshToken;
		}
		public string AccessToken { get; set; }

		public string RefreshToken { get; set; }

		public bool CanBeRefreshed => !string.IsNullOrEmpty(RefreshToken);
	}
}
