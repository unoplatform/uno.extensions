using Uno.Extensions.Reactive;

namespace Playground.Views;

public sealed partial class SkeletonFeedPage : Page
{
	// Long enough to see the skeleton, short enough to iterate on.
	private static readonly TimeSpan LoadDelay = TimeSpan.FromSeconds(2);

	private int _loadCount;

	public SkeletonFeedPage()
	{
		this.InitializeComponent();

		PeopleFeed.Source = CreateFeed();
	}

	private void Reload(object sender, RoutedEventArgs e) => PeopleFeed.Source = CreateFeed();

	private IFeed<Person[]> CreateFeed()
	{
		// Feed.Async caches feeds per delegate instance: capturing the load number yields a new delegate, hence a new feed.
		var load = ++_loadCount;

		return Feed.Async(async ct =>
		{
			await Task.Delay(LoadDelay, ct);

			return new Person[]
			{
				new("John Doe", $"Software Engineer (load #{load})"),
				new("Jane Smith", "Product Designer"),
				new("Alex Martin", "Engineering Manager"),
				new("Sam Lee", "QA Lead"),
			};
		});
	}

	public record Person(string Name, string Title);
}
