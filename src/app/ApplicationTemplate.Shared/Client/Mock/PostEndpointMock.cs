using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeneratedSerializers;
using Refit;

namespace ApplicationTemplate.Client
{
	public class PostEndpointMock : BaseMock, IPostEndpoint
	{
		public PostEndpointMock(IObjectSerializer serializer)
			: base(serializer)
		{
		}

		public Task<PostData> Create(CancellationToken ct, [Body] PostData post)
			=> Task.FromResult(post);

		public Task Delete(CancellationToken ct, [AliasAs("id")] long postId)
			=> Task.CompletedTask;

		public Task<PostData> Get(CancellationToken ct, [AliasAs("id")] long postId)
			=> GetTaskFromEmbeddedResource<PostData>();

		public Task<PostData[]> GetAll(CancellationToken ct)
			=> GetTaskFromEmbeddedResource<PostData[]>();

		public Task<PostData> Update(CancellationToken ct, [AliasAs("id")] long postId, [Body] PostData post)
			=> Task.FromResult(post);
	}
}
