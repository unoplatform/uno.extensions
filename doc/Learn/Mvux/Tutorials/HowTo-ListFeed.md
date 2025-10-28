---
uid: Uno.Extensions.Mvux.HowToListFeed
---

# MVUX List Feed

Quick guide for exposing a collection through `IListFeed<T>` and rendering it with `FeedView`.

## TL;DR
- Define a service that returns immutable collections (`IImmutableList<T>`).
- Surface the service call with `ListFeed.Async` inside your MVUX model; MVUX generates the bindable view model.
- Bind `FeedView.Source` to the list feed and present items via `ListView` (using `Data` and `Refresh`).
- Layer on filtering by introducing an `IState<TCriteria>` and projecting to a list feed with `Select(...).AsListFeed()`.

## 1. Service Contract
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

## 2. Expose the List Feed
```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IListFeed<Person> People =>
        ListFeed.Async(PeopleService.GetPeopleAsync);
}
```
- MVUX emits `PeopleViewModel` with a bindable `People` property.
- Await inside the model when needed: `var snapshot = await People;`.

## 3. Bind with FeedView
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
- `Data` supplies the latest immutable list.
- `Refresh` replays the feed request and shows the default progress state.

### Optional: Custom Progress Template
```xml
<mvux:FeedView.ProgressTemplate>
    <DataTemplate>
        <TextBlock Text="Loading people..." />
    </DataTemplate>
</mvux:FeedView.ProgressTemplate>
```

## 4. Add Filtering with State
```csharp
public partial record PersonCriteria(string? Term, bool IsDarkSideOnly)
{
    public bool Match(Person person) =>
        (string.IsNullOrEmpty(Term)
            || person.FirstName.Contains(Term, StringComparison.OrdinalIgnoreCase)
            || person.LastName.Contains(Term, StringComparison.OrdinalIgnoreCase))
        && (!IsDarkSideOnly || person.IsDarkSide);
}
```

Update the service to accept the criteria:
```csharp
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
```

Project the criteria into the list feed:
```csharp
public partial record PeopleModel(IPeopleService PeopleService)
{
    public IState<PersonCriteria> Criteria =>
        State.Value(this, () => new PersonCriteria(null, false));

    public IListFeed<Person> People =>
        Criteria
            .Select((criteria, ct) =>
                PeopleService.GetPeopleAsync(criteria, ct))
            .AsListFeed();
}
```
- `Select` reacts to criteria changes and replays the service call.
- `AsListFeed` converts the resulting feed of lists into an `IListFeed<Person>`.
- Bind criteria inputs with two-way bindings in XAML:
```xml
<TextBox Text="{Binding Criteria.Term, Mode=TwoWay}" />
<ToggleSwitch IsOn="{Binding Criteria.IsDarkSideOnly, Mode=TwoWay}" />
```

## Related Material
- Project setup: (xref:Uno.Extensions.Mvux.HowToMvuxProject)
- Feed basics: (xref:Uno.Extensions.Mvux.Overview)
- FeedView customization: (xref:Uno.Extensions.Mvux.FeedView)
- Sample app: https://github.com/unoplatform/Uno.Samples/tree/master/UI/MvuxHowTos/PeopleApp
