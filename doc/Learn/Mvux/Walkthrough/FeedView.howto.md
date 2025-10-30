---
uid: Uno.Extensions.Mvux.FeedView.HowTo
---
# Displaying asynchronous data with MVUX

**Goal:** render the current value of an `IFeed<T>` or `IState<T>` in XAML.

**Dependencies**

* NuGet: `Uno.Extensions.Reactive.UI`
* XAML namespace:

```xml
<Page
    ...
    xmlns:mvux="using:Uno.Extensions.Reactive.UI">
```

**Model**

```csharp
public partial record MainModel
{
    public IFeed<Person> CurrentContact => /* get from service */;
}
```

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</mvux:FeedView>
```

**Why it works**

* `Source` must be an `IFeed<T>` / `IState<T>` / list feed. ([Uno Platform][1])
* Inside the template, the **DataContext** is a `FeedViewState`, not the model.
* `FeedViewState.Data` is the actual value (`Person`, in this case). ([Uno Platform][1])

---

## Show list data from a feed

**Goal:** bind to a feed that returns a list and show items.

**XAML**

```xml
<mvux:FeedView Source="{Binding Contacts}">
    <DataTemplate>
        <ListView ItemsSource="{Binding Data}">
            <ListView.ItemTemplate>
                <DataTemplate>
                    <TextBlock Text="{Binding Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </DataTemplate>
</mvux:FeedView>
```

**Key point**

* For list feeds, `Data` is the collection to display. ([Uno Platform][1])

---

## Refresh feed data from inside the template

**Goal:** show current data and let the user refresh it.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <StackPanel Spacing="8">
            <TextBlock Text="{Binding Data.Name}" />
            <Button Content="Refresh contact"
                    Command="{Binding Refresh}" />
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```

**What happens**

* `Refresh` is a command on `FeedViewState`.
* Calling it tells the underlying feed to re-load from its source. ([Uno Platform][1])

---

## Refresh feed data from outside the template

**Goal:** put the refresh button somewhere else on the page.

**XAML**

```xml
<Grid>
    <mvux:FeedView x:Name="ContactFeed"
                   Source="{Binding CurrentContact}">
        <DataTemplate>
            <TextBlock Text="{Binding Data.Name}" />
        </DataTemplate>
    </mvux:FeedView>

    <Button Content="Refresh"
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            Command="{Binding Refresh, ElementName=ContactFeed}" />
</Grid>
```

**Why**

* The `FeedView` itself exposes the same `FeedViewState` so you can bind to it with `ElementName`. ([Uno Platform][1])

---

## Show loading while feed is fetching

**Goal:** replace the default progress ring with your own loading UI.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>

    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
            <StackPanel Spacing="4">
                <ProgressRing IsActive="True" />
                <TextBlock Text="Loading contact..." />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>
</mvux:FeedView>
```

**Notes**

* FeedView shows a progress UI when the feed is loading or refreshing. ([Uno Platform][1])
* You can turn it off / change behavior with `RefreshingState` (see below).

---

## Disable or change the default loading behavior

**Goal:** stop FeedView from showing the default progress ring.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}"
               RefreshingState="None">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</mvux:FeedView>
```

**Values**

* `None` – don’t show loading UI
* `Default` / `Loading` – use built-in behavior

**Why**

* `RefreshingState` accepts a `FeedViewRefreshState` enum value. ([Uno Platform][1])

---

## Show “no data” when the feed is empty

**Goal:** show a friendly empty state when the service returns null or an empty list.

**XAML**

```xml
<mvux:FeedView Source="{Binding Contacts}">
    <DataTemplate>
        <!-- real content -->
        <TextBlock Text="Contacts loaded." />
    </DataTemplate>

    <mvux:FeedView.NoneTemplate>
        <DataTemplate>
            <TextBlock Text="No contacts found." />
        </DataTemplate>
    </mvux:FeedView.NoneTemplate>
</mvux:FeedView>
```

**When it shows**

* Feed completed OK but returned `null`
* Feed completed OK but list is empty
* This is **not** an error state. ([Uno Platform][1])

---

## Show an error when the feed fails

**Goal:** display a custom UI if the feed throws an exception.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>

    <mvux:FeedView.ErrorTemplate>
        <DataTemplate>
            <StackPanel Spacing="8">
                <TextBlock Text="Something went wrong." />
                <Button Content="Try again" Command="{Binding Refresh}" />
                <TextBlock Text="{Binding Error.Message}" 
                           TextWrapping="Wrap" 
                           Opacity="0.5" />
            </StackPanel>
        </DataTemplate>
    </mvux:FeedView.ErrorTemplate>
</mvux:FeedView>
```

**Why**

* `FeedViewState.Error` is the exception from the feed. ([Uno Platform][1])
* You can still bind `Refresh` to retry.

---

## Show an initial “not loaded yet” state

**Goal:** show a placeholder before the feed starts loading.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>

    <mvux:FeedView.UndefinedTemplate>
        <DataTemplate>
            <TextBlock Text="Preparing data..." />
        </DataTemplate>
    </mvux:FeedView.UndefinedTemplate>
</mvux:FeedView>
```

**When it appears**

* When the page shows before the feed is actually invoked.
* Usually very short. ([Uno Platform][1])

---

## Get back to the page’s view model from inside the template

**Goal:** call properties/commands from the page VM, not from the feed.

**XAML**

```xml
<mvux:FeedView Source="{Binding CurrentContact}">
    <DataTemplate>
        <StackPanel Spacing="8">
            <TextBlock Text="{Binding Data.Name}" />

            <!-- parent = page VM -->
            <Button Content="Open details"
                    Command="{Binding Parent.OpenContactCommand}"
                    CommandParameter="{Binding Data}" />
        </StackPanel>
    </DataTemplate>
</mvux:FeedView>
```

**Why**

* `FeedViewState.Parent` is the DataContext of the `FeedView` itself (typically your page/view VM). ([Uno Platform][1])

---

## Reuse a template for value state

**Goal:** define the value template once and reuse it.

**XAML**

```xml
<Page.Resources>
    <DataTemplate x:Key="ContactTemplate">
        <TextBlock Text="{Binding Data.Name}" />
    </DataTemplate>
</Page.Resources>

<mvux:FeedView Source="{Binding CurrentContact}"
               ValueTemplate="{StaticResource ContactTemplate}" />
```

**Why**

* Anything inside `<FeedView>...</FeedView>` is actually its `ValueTemplate`. Setting `ValueTemplate` explicitly is equivalent. ([Uno Platform][1])

---

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Mvux/FeedView.html "The FeedView control "
