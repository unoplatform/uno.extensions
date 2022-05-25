using System;
using System.Threading;
using System.Threading.Tasks;
using Commerce.Data.Models;
using Commerce.Models;

namespace Commerce.Services;

public class ProfileService : IProfileService
{
	public async ValueTask<Profile> GetProfile(CancellationToken ct)
	{
		await Task.Delay(1, ct);
		var data = new ProfileData
		{
			FirstName = "Michael",
			LastName = "Scott",
			Avatar = "https://loremflickr.com/360/360/face"
		};

		return new Profile(data);
	}
}
