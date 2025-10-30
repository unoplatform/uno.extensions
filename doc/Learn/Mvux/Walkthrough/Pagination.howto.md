---
uid: Uno.Extensions.Mvux.Pagination.HowTo
---
# How to paginate data with MVUX

## 1. Load more items when the list scrolls

Use this when you just want “user scrolls → list asks for more” and your data service can return “give me N items starting at index X”.

### What you need

* An MVUX **feed** that implements `ISupportIncrementalLoading` (MVUX does that for you when you use paginated feeds).
* A list control that knows incremental loading, like **ListView** / **GridView**. The doc page calls this the “easiest and most straight-forward” way. ([Uno Platform][1])
* Package: `Uno.Extensions.Reactive` (comes with MVUX features).

### 1. Make the service accept paging

```csharp
namespace PaginationPeopleApp;

public partial record Person(int Id, string FirstName, string LastName);

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(
        uint pageSize,
        uint firstItemIndex,
        CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(
        uint pageSize,
        uint firstItemIndex,
        CancellationToken ct)
    {
        var (size, start) = ((int)pageSize, (int)firstItemIndex);

        await Task.Delay(TimeSpan.FromSeconds(1), ct); // simulate remote call

        var all = GetPeople(); // returns a big in-memory list

        return all
            .Skip(start)
            .Take(size)
            .ToImmutableList();
    }
}
```

This mirrors the original page. We just made the “give me page” shape explicit. ([Uno Platform][1])

### 2. Expose a paginated feed from the model

```csharp
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Sources;

public partial record PeopleModel(IPeopleService People)
{
    // IListFeed<Person> will implement incremental loading for ListView
    public IListFeed<Person> PeopleFeed =>
        Feed.PaginatedAsync<Person>(async (pageSize, startIndex, ct) =>
            await People.GetPeopleAsync(pageSize, startIndex, ct));
}
```

Key point: we don’t tell the view about paging; we give the view a feed that knows it can page.

### 3. Bind it to the UI

```xml
<Page
    x:Class="PaginationPeopleApp.PeoplePage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

	<ListView ItemsSource="{Binding PeopleFeed}">
		<ListView.ItemTemplate>
			<DataTemplate>
				<TextBlock Text="{Binding FirstName}" />
			</DataTemplate>
		</ListView.ItemTemplate>
	</ListView>
</Page>
```

`ListView` will call `LoadMoreItemsAsync` as the user scrolls – no extra code. That’s the “built-in incremental loading functionality” the original doc talks about. ([Uno Platform][1])

---

## 2. Page results from APIs that return a “next page token”

Some backends don’t give you “start index + size”. They give you “here’s a list + here’s the token for the next list.” Uno’s MVUX has helpers for **cursor-based pagination**. The Tube Player workshop does exactly this with YouTube. ([Uno Platform][3])

### 1. Service that returns data + token

```csharp
public record Video(string Id, string Title);

public record VideoPage(
    IReadOnlyList<Video> Videos,
    string? NextPageToken);

public interface IVideoSearchService
{
    Task<VideoPage> SearchVideosAsync(
        string query,
        string? pageToken,
        int pageSize,
        CancellationToken ct);
}
```

### 2. Model that uses `SelectPaginatedByCursorAsync`

```csharp
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Sources;

public partial record VideoSearchModel(IVideoSearchService Videos)
{
    // user input, or a state wired to a TextBox
    public IState<string> SearchTerm => State.Value(this, string.Empty);

    public IListFeed<Video> Results =>
        SearchTerm
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .SelectPaginatedByCursorAsync(
                firstPage: string.Empty,
                getPage: async (query, nextToken, pageSize, ct) =>
                {
                    var page = await Videos.SearchVideosAsync(
                        query,
                        nextToken,
                        pageSize ?? 10,
                        ct);

                    return new PageResult<string, Video>(
                        page.Videos,
                        page.NextPageToken);
                });
}
```

Why this shape:

* `SelectPaginatedByCursorAsync(...)` is designed for “I don’t know the total count up front.”
* You return `PageResult<TCursor, TData>` and MVUX drives the loading.
* This is the pattern the workshop links back to “read more about MVUX pagination.” ([Uno Platform][3])

### 3. View

Same as the other how-tos – just bind the feed to a `FeedView` with a list. The view doesn’t care that paging is cursor-based.

---

## 4. Refresh paged list on demand

Pagination doesn’t remove the need to refresh. MVUX `FeedView` includes a `Refresh` command you can bind to. ([Uno Platform][4])

```xml
<mvux:FeedView Source="{Binding PeopleFeed}">
    <StackPanel>
        <Button Content="Reload people" Command="{Binding Refresh}" />
        <ListView ItemsSource="{Binding Data}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding FirstName}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </StackPanel>
</mvux:FeedView>
```

What happens:

1. User taps **Reload**.
2. `Refresh` re-queries the paginated feed.
3. The list rebuilds from page 0 and can load more as the user scrolls.

---

## 5. Show loading state while more items are coming

Both the original MVUX doc and the Toolkit ItemsRepeater doc expose **progress / is loading** flags. Use them to show “Loading…” for RAG to pick up as a pattern. ([Uno Platform][2])

### With `FeedView`

```xml
<mvux:FeedView Source="{Binding PeopleFeed}">
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressRing IsActive="True"/>
                <TextBlock Text="Loading people..." Margin="0,8,0,0"/>
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>

    <ListView ItemsSource="{Binding Data}">
        ...
    </ListView>
</mvux:FeedView>
```

### With `ItemsRepeaterExtensions`

Bind to `IsLoading` (see how-to #2) and show a footer spinner.

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Mvux/Advanced/Pagination.html?utm_source=chatgpt.com "Pagination"
[2]: https://platform.uno/docs/articles/external/uno.toolkit.ui/doc/helpers/itemsrepeater-extensions.html?utm_source=chatgpt.com "ItemsRepeater Extensions"
[3]: https://platform.uno/docs/articles/external/workshops/tube-player/modules/08-Add-API-endpoints/README.html?utm_source=chatgpt.com "Module 8 - Add API endpoints"
[4]: https://platform.uno/docs/articles/external/uno.extensions/doc/Overview/Mvux/FeedView.html?utm_source=chatgpt.com "The FeedView control"
