using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace TestBackend.Controllers;

[ApiController]
[Route("[controller]")]
[EnableCors("AllowAll")]
public class KiotaController : ControllerBase
{
	private static readonly List<string> _testData = ["Item1", "Item2", "Item3"];

	[AllowAnonymous]
	[HttpPost("login")]
	public IActionResult Login([FromBody] LoginRequest request)
	{
		if (request.Username == "testuser" && request.Password == "password")
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes("SuperSecureVeryLongSecretKey12345");

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, request.Username) }),
				Expires = DateTime.UtcNow.AddHours(1),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			var tokenString = tokenHandler.WriteToken(token);

			return Ok(new { AccessToken = tokenString });
		}

		return Unauthorized(new { Message = "Invalid credentials" });
	}

	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[HttpGet("data")]
	[ProducesResponseType(typeof(DataResponse), 200)]
	public IActionResult GetData()
	{
		var username = User.Identity?.Name;
		var response = new DataResponse
		{
			Data = _testData,
			Message = "Authenticated request",
			Username = username ?? "Unknown"
		};
		return Ok(response);
	}
}

public class LoginRequest
{
	public string Username { get; set; }
	public string Password { get; set; }
}
public class DataResponse
{
	public List<string> Data { get; set; } = new();
	public string Message { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
}

