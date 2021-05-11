using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace ApplicationTemplate.Client
{
    [Headers("Authorization: Bearer")]
    public interface IUserProfileEndpoint
    {
        /// <summary>
        /// Returns the current user profile.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <returns><see cref="UserProfileData"/></returns>
        [Get("/me")]
        Task<UserProfileData> Get(CancellationToken ct);

        /// <summary>
        /// Updates the current user profile.
        /// </summary>
        /// <param name="ct"><see cref="CancellationToken"/></param>
        /// <param name="userProfile">User profile</param>
        /// <returns><see cref="UserProfileData"/></returns>
        [Put("/me")]
        Task Update(CancellationToken ct, UserProfileData userProfile);
    }
}
