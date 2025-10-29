---
uid: Uno.Extensions.Mvux.HowToListFeed
---

# MVUX List Feed

Use MVUX list feeds to load immutable collections, show them through `FeedView`, and react to user input.

## Load people from a service

```csharp
namespace PeopleApp;

public partial record Person(string FirstName, string LastName, bool IsDarkSide);

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var people = new[]
        {
            new Person("Master", "Yoda", false),
            new Person("Darth", "Vader", true)
        };

        return people.ToImmutableList();
    }
}
```

Records keep the payload immutable and easy to clone.

## Expose the collection through the model

```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed.Async(PeopleService.GetPeopleAsync);
}
```

`ListFeed.Async` turns the async service call into a feed that MVUX can data-bind.

## Show the list in the UI

```xml
<Page ...
      xmlns:mvux="using:Uno.Extensions.Reactive.UI">
    <mvux:FeedView Source="{Binding People}">
        <DataTemplate>
            <ListView ItemsSource="{Binding Data}">
                <ListView.Header>
                    <Button Content="Refresh" Command="{Binding Refresh}" />
                </ListView.Header>
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal" Spacing="8">
                            <TextBlock Text="{Binding FirstName}" />
                            <TextBlock Text="{Binding LastName}" />
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </DataTemplate>
    </mvux:FeedView>
</Page>
```

`Data` exposes the current immutable snapshot; `Refresh` replays the service call.

## Customize the loading state

```xml
<mvux:FeedView.ProgressTemplate>
    <DataTemplate>
        <TextBlock Text="Loading people..." />
    </DataTemplate>
</mvux:FeedView.ProgressTemplate>
```

Override progress, error, none, or undefined templates to align with your UI.

## Filter people with state input

```csharp
public partial record PersonCriteria(string? Term, bool IsDarkSideOnly)
{
    public bool Match(Person person) =>
        (string.IsNullOrEmpty(Term)
            || person.FirstName.Contains(Term, StringComparison.OrdinalIgnoreCase)
            || person.LastName.Contains(Term, StringComparison.OrdinalIgnoreCase))
        && (!IsDarkSideOnly || person.IsDarkSide);
}

public interface IPeopleService
{
    ValueTask<IImmutableList<Person>> GetPeopleAsync(PersonCriteria criteria, CancellationToken ct);
}

public class PeopleService : IPeopleService
{
    public async ValueTask<IImmutableList<Person>> GetPeopleAsync(PersonCriteria criteria, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        var people = new[]
        {
            new Person("Master", "Yoda", false),
            new Person("Darth", "Vader", true)
        };

        return people.Where(criteria.Match).ToImmutableList();
    }
}

public partial record PeopleModel(IPeopleService PeopleService)
{
    public IState<PersonCriteria> Criteria =>
        State.Value(this, () => new PersonCriteria(null, false));

    public IListFeed<Person> People =>
        Criteria
            .Select((criteria, ct) => PeopleService.GetPeopleAsync(criteria, ct))
            .AsListFeed();
}
```

`Select` reacts to changes in state and reissues the list feed with new results.

## Bind filtering inputs

```xml
<StackPanel>
    <TextBox Text="{Binding Criteria.Term, Mode=TwoWay}" />
    <ToggleSwitch IsOn="{Binding Criteria.IsDarkSideOnly, Mode=TwoWay}" />
</StackPanel>
```

Two-way bindings keep the criteria state in sync with user input.

## Resources

- Project setup: (xref:Uno.Extensions.Mvux.HowToMvuxProject)
- Feed basics: (xref:Uno.Extensions.Mvux.Overview)
- FeedView customization: (xref:Uno.Extensions.Mvux.FeedView)
- Sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/PeopleApp
