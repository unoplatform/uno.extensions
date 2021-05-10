using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace ApplicationTemplate.Client
{
	[Headers("Authorization: Bearer")]
	public interface IPostEndpoint
	{
		/// <summary>
		/// Gets the list of all posts.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <returns>List of posts</returns>
		[Get("/posts")]
		Task<PostData[]> GetAll(CancellationToken ct);

		/// <summary>
		/// Gets the post of the specified id.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <returns>Post</returns>
		[Get("/posts/{id}")]
		Task<PostData> Get(CancellationToken ct, [AliasAs("id")] long postId);

		/// <summary>
		/// Creates a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="post">Post</param>
		/// <returns>New <see cref="PostData"/></returns>
		[Post("/posts")]
		Task<PostData> Create(CancellationToken ct, [Body] PostData post);

		/// <summary>
		/// Updates a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <param name="post">Post</param>
		/// <returns>Updated <see cref="PostData"/></returns>
		[Put("/posts/{id}")]
		Task<PostData> Update(CancellationToken ct, [AliasAs("id")] long postId, [Body] PostData post);

		/// <summary>
		/// Delets a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <returns><see cref="Task"/></returns>
		[Delete("/posts/{id}")]
		Task Delete(CancellationToken ct, [AliasAs("id")] long postId);
	}
}
