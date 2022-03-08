//-:cnd:noEmit
using System;
using System.Linq;

namespace MyExtensionsApp.ViewModels;

public record Credentials
{
	public string UserName { get; set; }

	public string Password { get; set; }
}
