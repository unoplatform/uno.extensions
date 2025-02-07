using System.IdentityModel.Tokens.Jwt;
using TestHarness.Ext.Http.Kiota.Client;
using Uno.Extensions.Http.Kiota;
using Uno.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace TestHarness.Ext.Http.Kiota;

public class KiotaHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => ["TestHarness.Ext.Http.Kiota.appsettings.json"];

	protected override IHostBuilder Custom(IHostBuilder builder) =>
		builder
			.UseAuthentication(auth =>
				auth.AddCustom(custom =>
					custom.Login((sp, dispatcher, credentials, cancellationToken) =>
					{
						var key = Encoding.UTF8.GetBytes("SuperSecureVeryLongSecretKey12345");
						var tokenDescriptor = new SecurityTokenDescriptor
						{
							Subject = new ClaimsIdentity(new[] { new Claim("sub", "testuser") }),
							Expires = DateTime.UtcNow.AddMinutes(30),
							SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
						};

						var tokenHandler = new JwtSecurityTokenHandler();
						var securityToken = tokenHandler.CreateToken(tokenDescriptor);
						var jwtToken = tokenHandler.WriteToken(securityToken);
						var tokenDictionary = new Dictionary<string, string>
						{
							{ TokenCacheExtensions.AccessTokenKey, jwtToken},
							{ TokenCacheExtensions.RefreshTokenKey, "DummyRefreshToken" },
							{ "Expiry", DateTime.UtcNow.AddMinutes(30).ToString("o") }
						};
						return ValueTask.FromResult<IDictionary<string, string>?>(tokenDictionary);
					})
				)
			)
			.ConfigureServices((context, services) =>
			{
				services.AddKiotaClient<KiotaTestClient>(context, options: new EndpointOptions { Url = "https://localhost:7193" });
				services.AddTransient<KiotaHomeViewModel>();

			});

	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<KiotaMainPage>(),
			new ViewMap<KiotaHomePage, KiotaHomeViewModel>()
		);

		routes.Register(
			new RouteMap("", View: views.FindByView<KiotaMainPage>()),
			new RouteMap("Home", View: views.FindByViewModel<KiotaHomeViewModel>())
		);
	}
}
