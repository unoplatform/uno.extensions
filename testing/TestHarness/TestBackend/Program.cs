// Setup of Program.cs and WebAuthController comes from https://github.com/dotnet/maui/tree/main/src/Essentials/samples/Sample.Server.WebAuthenticator

using System.Net.NetworkInformation;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
		policy.AllowAnyOrigin()
			.AllowAnyMethod()
			.AllowAnyHeader());
});

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
					o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
					o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				}).AddJwtBearer(options =>
				{
					options.RequireHttpsMetadata = false;
					options.SaveToken = true;
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecureVeryLongSecretKey12345")),
						ValidateIssuer = false,
						ValidateAudience = false
					};
				})
		
		//.AddFacebook(fb =>
		//{
		//	fb.AppId = auth.FacebookAppId!;
		//	fb.AppSecret = auth.FacebookAppSecret!;
		//	fb.SaveTokens = true;
		//})
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

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();




app.MapControllers();

app.Run();
