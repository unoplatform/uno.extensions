---
uid: Uno.Extensions.Mvux.Messaging.HowTo
---

# How to use messaging with MVUX

## Problem

You have an `IListState<T>` (for example, a list of people) and a service that creates/updates/deletes those entities. You want the UI to refresh automatically when the service changes data, without the service holding a reference to the MVUX model.

## When to use

* You already use **CommunityToolkit.Mvvm.Messaging** to broadcast changes.
* Your UI shows a **list** (`IListState<T>` / `ListState.Async(...)`).
* You want **create / update / delete** to flow to the UI.

Requires the `MVUX` UnoFeature.

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public record Person(string Name);

public partial record PeopleModel
{
    private IPeopleService PeopleService { get; }

    // 1. normal MVUX list
    public IListState<Person> People =>
        ListState.Async(this, PeopleService.GetAllAsync);

    public PeopleModel(IPeopleService peopleService, IMessenger messenger)
    {
        PeopleService = peopleService;

        // 2. listen to EntityMessage<Person> and apply to People
        messenger.Observe(
            state: People,
            keySelector: person => person.Name);
    }
}
```

### How it works

* The model subscribes to **entity-change messages** of type `EntityMessage<Person>`.
* The key selector (`person => person.Name`) tells MVUX how to match an incoming entity with an existing one. Pick something unique (Id, Email, etc.). ([Uno Platform][1])
* When the service sends `Created`, the item is added; `Updated` replaces it; `Deleted` removes it — all on the list state.

#### Notes

* This keeps the **service** and the **model** decoupled: only messages travel between them. ([Uno Platform][1])
* Use this pattern when multiple screens can change the same data and you want all of them to stay current.

---

## 02-send-entity-change-from-service.md

### Title

**Broadcast entity changes from a service**

### Problem

You changed data in a service (for example, created a `Person`) and need every MVUX model that shows people to refresh.

Requires the `MVUX` UnoFeature.

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive.Messaging;

public class PeopleService : IPeopleService
{
    private readonly IPeopleRepository _repo;
    private readonly IMessenger _messenger;

    public PeopleService(IPeopleRepository repo, IMessenger messenger)
    {
        _repo = repo;
        _messenger = messenger;
    }

    public async ValueTask<IImmutableList<Person>> GetAllAsync(CancellationToken ct)
        => (await _repo.GetAllPeople(ct)).ToImmutableList();

    public async ValueTask CreateAsync(Person person, CancellationToken ct)
    {
        var created = await _repo.CreateAsync(person, ct);

        // tell everyone: a Person was CREATED
        _messenger.Send(new EntityMessage<Person>(
            EntityChange.Created,
            created));
    }
}
```

#### What to send

* `EntityChange.Created` – list states will **add** it.
* `EntityChange.Updated` – list/single states will **replace** it.
* `EntityChange.Deleted` – list states will **remove** it, single states will **clear**. ([Uno Platform][1])

#### Why

Any model that called `messenger.Observe(...)` for `Person` will update itself immediately. No direct references.

---

## 03-register-the-messenger-in-the-app.md

### Title

**Register the Community Toolkit messenger in DI**

### Problem

Your models and services need `IMessenger`, but nothing is providing it.

Requires the `MVUX` and `MVVM` UnoFeature.

```csharp
// in App.xaml.cs or your Startup/Host builder
using CommunityToolkit.Mvvm.Messaging;

public sealed partial class App : Application
{
    public App()
    {
        this.InitializeComponent();

        var builder = this.CreateAppBuilder();
        builder.Services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        // ... other services
    }
}
```

#### Why `WeakReferenceMessenger`

* It’s the recommended default in the original doc.
* It prevents hard references between publishers and subscribers. ([Uno Platform][1])

---

## 04-listen-to-custom-messages-in-a-model.md

### Title

**React to a custom message in an MVUX model**

### Problem

You want to listen to *your own* message type (not entity-change) and run code in the model.

```csharp
using CommunityToolkit.Mvvm.Messaging;

public record MyMessage(string Text);

public partial record MyModel
{
    private IMessenger Messenger { get; }

    public MyModel(IMessenger messenger)
    {
        Messenger = messenger;

        Messenger.Register<MyModel, MyMessage>(this,
            static (recipient, message) =>
            {
                recipient.OnMessage(message);
            });
    }

    private void OnMessage(MyMessage message)
    {
        // do something: refresh, navigate, update state, etc.
    }
}
```

#### Sender

```csharp
public class AnyOtherService
{
    private readonly IMessenger _messenger;

    public AnyOtherService(IMessenger messenger)
        => _messenger = messenger;

    public void Notify(string value)
        => _messenger.Send(new MyMessage(value));
}
```

This is the basic Community Toolkit pattern that the MVUX bits build on. ([Uno Platform][1])

---

## 05-keep-a-single-state-in-sync.md

### Title

**Keep a single state in sync with messenger updates**

### Problem

You don’t have a list — just one `User`, `Order`, or `Settings` object — and you want it to refresh when the service broadcasts an update. ([Uno Platform][1])

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public partial record ProfileModel
{
    public IState<User> CurrentUser =>
        State.Async(this, _userService.GetCurrentUser);

    private readonly IUserService _userService;

    public ProfileModel(IUserService userService, IMessenger messenger)
    {
        _userService = userService;

        // if an EntityMessage<User> arrives, update this single state
        CurrentUser.Observe(
            messenger,
            user => user.Id);
    }
}
```

#### What happens

* If the messenger sends `EntityMessage<User>(Updated, userX)` and `userX.Id` matches the current state’s user, the state updates.
* If the messenger sends `EntityMessage<User>(Deleted, userX)`, the state is **cleared**. ([Uno Platform][1])

---

## 06-use-fluent-observe-and-dispose.md

### Title

**Start and stop observing messages**

### Problem

You need to **unsubscribe** (for example, view lifetime or manual teardown).

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive.Messaging;

public partial record UserModel
{
    private readonly IDisposable _subscription;

    public IState<User> CurrentUser => ...;

    public UserModel(IUserService service, IMessenger messenger)
    {
        CurrentUser
            .Observe(messenger, user => user.Id, out _subscription);
    }

    // call when done (e.g. Dispose, Close)
    private void Stop()
        => _subscription.Dispose();
}
```

* This uses the fluent overload that gives you an `IDisposable`. ([Uno Platform][1])
* Same idea applies to `IListState<T>`.

---

## 07-apply-entity-message-manually.md

### Title

**Apply an entity message to a state yourself**

### Problem

You already have an `EntityMessage<T>` instance (maybe from a custom channel or a filtered messenger) and you want to apply it to a state/list without going through `Observe(...)`.

```csharp
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public static class MessageHelpers
{
    public static void ApplyToList(
        IListState<Person> list,
        EntityMessage<Person> message)
    {
        list.Update(message, person => person.Name);
    }

    public static void ApplyToSingle(
        IState<Person> state,
        EntityMessage<Person> message)
    {
        state.Update(message, person => person.Name);
    }
}
```

* `Update(...)` is provided by the same messaging package. ([Uno Platform][1])
* Use this when you do extra processing before updating the MVUX state.

---

## 08-choose-the-right-key.md

### Title

**Pick the correct key for entity matching**

### Problem

Your state isn’t updating after messages arrive.

### Rule of thumb

* **Use a real identifier**: `person => person.Id`
* Only use `Name`, `Title`, etc., if they are guaranteed unique in your data source. The doc sample uses `Name` just to stay short. ([Uno Platform][1])
* If the key changes (e.g. user can rename), then:

  * either send an **Updated** with the **new** key,
  * or pick a key that never changes (like database Id).

[1]: https://platform.uno/docs/articles/external/uno.extensions/doc/Learn/Mvux/Advanced/Messaging.html "Messaging "
