using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using GeneratedSerializers;

namespace ApplicationTemplate.Client
{
	public class UserProfileEndpointMock : BaseMock, IUserProfileEndpoint
	{
		private readonly IApplicationSettingsService _applicationSettingsService;

		public UserProfileEndpointMock(
			IObjectSerializer serializer,
			IApplicationSettingsService applicationSettingsService
		) : base(serializer)
		{
			_applicationSettingsService = applicationSettingsService ?? throw new ArgumentNullException(nameof(applicationSettingsService));
		}

		public async Task<UserProfileData> Get(CancellationToken ct)
		{
			var settings = await _applicationSettingsService.GetAndObserveCurrent().FirstAsync(ct);

			if (settings.AuthenticationData == default(AuthenticationData))
			{
				return default(UserProfileData);
			}

			await Task.Delay(TimeSpan.FromSeconds(2));

			return await GetTaskFromEmbeddedResource<UserProfileData>();
		}

		public async Task Update(CancellationToken ct, UserProfileData userProfile)
		{
			await Task.Delay(TimeSpan.FromSeconds(2));
		}
	}
}
