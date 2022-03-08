//-:cnd:noEmit
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyExtensionsApp.Models;

namespace MyExtensionsApp.Services;

public class ProfileService : IProfileService
{
	public async ValueTask<Profile> GetProfile(CancellationToken ct)
	{
		await Task.Delay(1, ct);
		return new Profile
		{
			FirstName = "Michael",
			LastName = "Scott",
			Avatar = "https://loremflickr.com/360/360/face"
		};
	}
}
