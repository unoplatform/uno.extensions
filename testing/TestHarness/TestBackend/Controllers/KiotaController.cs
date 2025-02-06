using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace TestBackend.Controllers;

[ApiController]
[Route("[controller]")]
[EnableCors("AllowAll")]
public class KiotaController : ControllerBase
{
	private static readonly List<string> _testData = ["Item1", "Item2", "Item3"];

	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[HttpGet("data")]
	[ProducesResponseType(typeof(DataResponse), 200)]
	public IActionResult GetData()
	{
		var token = Request.Headers["Authorization"].ToString();

		if (string.IsNullOrEmpty(token))
		{
			return Unauthorized(new { Message = "Missing Authorization header." });
		}

		var response = new DataResponse
		{
			Data = _testData,
			Message = "Authenticated request",
			Token = token
		};
		return Ok(response);
	}
}

public class DataResponse
{
	public List<string> Data { get; set; } = new();
	public string Message { get; set; } = string.Empty;
	public string Token { get; set; } = string.Empty;
}
