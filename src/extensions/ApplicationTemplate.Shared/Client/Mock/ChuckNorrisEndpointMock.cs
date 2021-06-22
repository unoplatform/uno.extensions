using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using Uno.Extensions.Serialization;

namespace ApplicationTemplate.Client
{
    public class ChuckNorrisEndpointMock : BaseMock, IChuckNorrisEndpoint
    {
        public ChuckNorrisEndpointMock(ISerializer serializer)
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
