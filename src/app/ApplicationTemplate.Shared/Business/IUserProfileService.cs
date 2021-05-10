using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;

namespace ApplicationTemplate.Business
{
	public interface IUserProfileService
	{
		/// <summary>
		/// Gets the current user profile.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <returns><see cref="UserProfileData"/></returns>
		Task<UserProfileData> GetCurrent(CancellationToken ct);

		/// <summary>
		/// Updates the current user profile.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="userProfile"><see cref="UserProfileData"/></param>
		/// <returns><see cref="Task"/></returns>
		Task Update(CancellationToken ct, UserProfileData userProfile);
	}
}
