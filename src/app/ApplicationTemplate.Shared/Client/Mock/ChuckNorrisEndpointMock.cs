using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeneratedSerializers;
using Refit;

namespace ApplicationTemplate.Client
{
	public class ChuckNorrisEndpointMock : BaseMock, IChuckNorrisEndpoint
	{
		public ChuckNorrisEndpointMock(IObjectSerializer serializer)
			: base(serializer)
		{
		}

		public async Task<ChuckNorrisResponse> Search(CancellationToken ct, [AliasAs("query")] string searchTerm)
		{
			await Task.Delay(TimeSpan.FromSeconds(2));

			return await GetTaskFromEmbeddedResource<ChuckNorrisResponse>();
		}
	}
}
