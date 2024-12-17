using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestHarness.Ext.Http.Kiota.Client;
using Uno.Extensions.Navigation;

namespace TestHarness.Ext.Http.Kiota;

public class KiotaHomeViewModel
{
	private readonly PostsApiClient _postsApiClient;

	public string FetchPostsResult { get; set; } = string.Empty;

	public KiotaHomeViewModel(PostsApiClient postsApiClient)
	{
		_postsApiClient = postsApiClient;
	}

	public async void FetchPosts()
	{
		try
		{
			var posts = await _postsApiClient.Posts.GetAsync();
			FetchPostsResult = $"Retrieved {posts?.Count} posts.";
		}
		catch (Exception ex)
		{
			FetchPostsResult = "Failed to fetch posts.";
		}
	}
}
