using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;

namespace ApplicationTemplate.Business
{
	public partial class PostService : IPostService
	{
		private readonly IPostEndpoint _postEndpoint;

		public PostService(IPostEndpoint postEndpoint)
		{
			_postEndpoint = postEndpoint;
		}

		public async Task<PostData> GetPost(CancellationToken ct, long postId)
		{
			return await _postEndpoint.Get(ct, postId);
		}

		public async Task<ImmutableList<PostData>> GetPosts(CancellationToken ct)
		{
			var posts = await _postEndpoint.GetAll(ct);

			return posts.ToImmutableList();
		}

		public async Task<PostData> Create(CancellationToken ct, PostData post)
		{
			return await _postEndpoint.Create(ct, post);
		}

		public async Task<PostData> Update(CancellationToken ct, long postId, PostData post)
		{
			return await _postEndpoint.Update(ct, postId, post);
		}

		public async Task Delete(CancellationToken ct, long postId)
		{
			await _postEndpoint.Delete(ct, postId);
		}
	}
}
