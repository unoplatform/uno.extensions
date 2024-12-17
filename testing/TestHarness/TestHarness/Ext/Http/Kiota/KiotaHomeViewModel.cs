using TestHarness.Ext.Http.Kiota.Client;
using Uno.Extensions.Navigation;

namespace TestHarness.Ext.Http.Kiota;

[ReactiveBindable(false)]
public partial class KiotaHomeViewModel : ObservableObject
{
	private readonly PostsApiClient _postsApiClient;
	private readonly INavigator _navigator;

	[ObservableProperty]
	private string _fetchPostsResult = string.Empty;

	[ObservableProperty]
	private string _initializationStatus = string.Empty;

	public KiotaHomeViewModel(PostsApiClient postsApiClient, INavigator navigator)
	{
		_postsApiClient = postsApiClient;
		_navigator = navigator;

		InitializationStatus = "Kiota Client initialized successfully.";
	}

	public async void FetchPosts()
	{
		try
		{
			FetchPostsResult = "Fetching posts...";
			var posts = await _postsApiClient.Posts.GetAsync();
			FetchPostsResult = $"Retrieved {posts?.Count} posts.";
		}
		catch (Exception ex)
		{
			FetchPostsResult = "Failed to fetch posts.";
		}
	}
}
