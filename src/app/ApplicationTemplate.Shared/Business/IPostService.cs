using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Client;

namespace ApplicationTemplate.Business
{
	public interface IPostService
	{
		/// <summary>
		/// Gets the post of the specified id.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <returns><see cref="PostData"/></returns>
		Task<PostData> GetPost(CancellationToken ct, long postId);

		/// <summary>
		/// Gets the list of all posts.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <returns>List of all posts</returns>
		Task<ImmutableList<PostData>> GetPosts(CancellationToken ct);

		/// <summary>
		/// Creates a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="post"><see cref="PostData"/></param>
		/// <returns>New <see cref="PostData"/></returns>
		Task<PostData> Create(CancellationToken ct, PostData post);

		/// <summary>
		/// Updates a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <param name="post"><see cref="PostData"/></param>
		/// <returns>Updated <see cref="PostData"/></returns>
		Task<PostData> Update(CancellationToken ct, long postId, PostData post);

		/// <summary>
		/// Deletes a post.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <param name="postId">Post id</param>
		/// <returns><see cref="Task"/></returns>
		Task Delete(CancellationToken ct, long postId);
	}
}
