// Setup of Program.cs and WebAuthController comes from https://github.com/dotnet/maui/tree/main/src/Essentials/samples/Sample.Server.WebAuthenticator

using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//new ConfigureFromConfigurationOptions<AuthConfiguration>(builder.Configuration.GetSection("Auth")).Configure(mySettings);
var auth = new Auth();
var authSection = builder.Configuration.GetSection(nameof(Auth));
ConfigurationBinder.Bind(authSection, auth);
builder.Services.Configure<Auth>(authSection);

builder.Services
		.AddAuthentication(o =>
				{
					o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				})
		.AddCookie()
		.AddFacebook(fb =>
		{
			fb.AppId = auth.FacebookAppId!;
			fb.AppSecret = auth.FacebookAppSecret!;
			fb.SaveTokens = true;
		})
		//.AddGoogle(g =>
		//{
		//	g.ClientId = auth.GoogleClientId!;
		//	g.ClientSecret = auth.GoogleClientSecret!;
		//	g.SaveTokens = true;
		//})
		//.AddMicrosoftAccount(ms =>
		//{
		//	ms.ClientId = auth.MicrosoftClientId!;
		//	ms.ClientSecret = auth.MicrosoftClientSecret!;
		//	ms.SaveTokens = true;
		//})
		//.AddApple(a =>
		//{
		//	a.ClientId = auth.AppleClientId!;
		//	a.KeyId = auth.AppleKeyId;
		//	a.TeamId = auth.AppleTeamId!;
		//	a.UsePrivateKey(keyId
		//		=> builder.Environment.ContentRootFileProvider.GetFileInfo($"AuthKey_{keyId}.p8"));
		//	a.SaveTokens = true;
		//})
		;
/*
* For Apple signin
* If you are running the app on Azure App Service you must add the Configuration setting
* WEBSITE_LOAD_USER_PROFILE = 1
* Without this setting you will get a File Not Found exception when AppleAuthenticationHandler tries to generate a certificate using your AuthKey_{keyId}.p8 file.
*/


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

//app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();




app.MapControllers();

app.Run();
