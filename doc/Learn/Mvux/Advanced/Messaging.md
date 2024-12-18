---
uid: Uno.Extensions.Mvux.Advanced.Messaging
---

# Messaging

Messaging is the ability to send in-app messages between its components to enable them to remain decoupled from one another.

In MVUX, we use `Feeds` to pull entities from a service. When executing a command or action, we call methods on the service that apply changes to the data, such as entity creation, removal, or update.
However, when the service changes the data, it's not a one-way street. We need to notify the feed that a change has occurred and that it should update the affected entities. But we also want to maintain the decoupling. The service shouldn't have a reference to the model or know about it; it's the model that uses the service, not the other way around.

Here is where messaging comes in handy. The service sends a message about this entity change to a central messenger that publishes messages to anyone willing to listen. The model then subscribes to the messages it wants to listen to (filtered by type of entity and entity key) and updates its feeds with the updated entities received from the service.

## Community Toolkit messenger

The Community Toolkit messenger is a standard tool for sending and receiving messages between app objects. It enables objects to remain decoupled from each other without keeping a strong reference between the sender and the receiver. Messages can also be sent over specific channels uniquely identified by a token or within certain application areas.

> [!NOTE]
> To ensure that the Community Toolkit Messenger works correctly, it is essential to register the `IMessenger` service in your app using the following code in `App.xaml.cs`:
>
> ```csharp
>   services.AddSingleton<IMessenger, WeakReferenceMessenger>();
> ```

The core component of the messenger is the `IMessenger` object. Its primary methods are `Register` and `Send.` `Register` subscribes to an object to start listening to messages of a specific type, whereas `Send` sends messages to all listening parties.
There are various ways to obtain the `IMessenger` object. Still, we'll use the most common one, which involves using [Dependency Injection](xref:Uno.Extensions.DependencyInjection.Overview) (DI) to register the `IMessenger` service in the app so it can then be resolved when other dependent types (e.g., ViewModels) are constructed.

In the model, we obtain a reference to the `IMessenger` on the constructor, which the DI's service provider resolves.
The `Register` method has quite a few overloads, but for the sake of this example, let's use the one that takes the recipient and the message type as parameters. The first parameter is the recipient (`this` in this case), and the second is a callback executed when a message has been received. Although `this` can be called within the callback, it's preferred that the callback doesn't make external references and that the `MyModel` is passed in as an argument.
`MessageReceived` is then called on the recipient (the current `MyModel`), passing the message to it.

MVUX includes extension methods that enable the integration between the [Community Toolkit messenger](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/messenger) and [MVUX feeds](xref:Uno.Extensions.Mvux.Feeds). But before discussing how MVUX integrates with the Community Toolkit messenger, let's quickly look at how the messenger works.

```csharp
using CommunityToolkit.Mvvm.Messaging;

public partial record MyModel
{
    protected IMessenger Messenger { get; }

    public MyModel(IMessenger messenger)
    {
        this.Messenger = messenger;

        this.Messenger.Register<MyModel, MyMessage>(this, (recipient, myMessage) =>
        {
            recipient.MessageReceived(myMessage);
        });
    }

    public void MessageReceived(MyMessage myMessage)
    {
        ...
    }
}
```

The `MyMessage` type is defined in a location accessible to both the sender and the receiver:

```csharp
public record MyMessage(string Message);
```

Then, in a service or any other module in the app, a `MyMessage` is sent:

```csharp
using CommunityToolkit.Mvvm.Messaging;

public partial record AnotherModelOrService
{
    public AnotherModel(IMessenger messenger)
    {
        this.Messenger = messenger;
    }

    protected IMessenger Messenger { get; }

    public IFeed<string> Message => ...

    public void SendMessage(string message)
    {
        var myMessenge = new MyMessage(message);
        Messenger.Send(myMessage);
    }
}
```

## MVUX integration with the messenger

MVUX harnesses the power of the Community Toolkit messenger and adds extension methods that enable you to listen to entity changes received from the messenger and have them automatically applied to the state or list-state storing the entities in the current model. The following entity-change types are supported: created, updated, and deleted.

For instance, when a command in the model creates a new entity and stores it in the database using a service, the service can send a 'created' entity-change message to the messenger, which can then be intercepted in the model to have the `State` or `ListState` update itself and display the newly created entity received from the messenger.

These extensions are part of the [`Uno.Extensions.Reactive.Messaging`](https://www.nuget.org/packages/Uno.Extensions.Reactive.Messaging) NuGet package.

### Observe

The purpose of the `Observe` methods (it comes in several overloads, [see below](#additional-observe-overloads)) is to intercept entity-change messages (`EntityMessage<T>`) from the Community Toolkit messenger and apply them to the designated state or list-state.

In the example below, a model displays a `StateList` of `Person` entities received from a service, loaded using a `State` with the [`Async`](xref:Uno.Extensions.Mvux.ListStates#async) factory method.

As you can gather from the code, the service interacts with an external source to load and save `Person` data. In the example below, we can see the use of two of its methods: `GetAllAsync` and `CreateNameAsync`.

There's also the `CreateNewPerson` method, which gets generated as a command in the ViewModel and can be invoked from the View (refer to [commands](xref:Uno.Extensions.Mvux.Advanced.Commands) to learn about how MVUX generates commands). This method uses `CreateRandomName`, which generates a random name (implementation removed for brevity).

The line using the MVUX messaging extension method is the one calling `messenger.Observe` . Read the code, and this line will be explained later.

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public record Person(string Name);

public partial record PeopleModel
{
    protected IPeopleService PeopleService { get; }

    public IListState<Person> People => ListState.Async(this, PeopleService.GetAllAsync);

    public PeopleModel(IPeopleService peopleService, IMessenger messenger)
    {
        PeopleService = peopleService;

        messenger.Observe(state: People, keySelector: person => person.Name);
    }

    public async TaskValue CreateNewPerson(CancellationToken ct)
    {
        var randomName = CreateRandomName();
        var newPerson = new Person(randomName);

        await PeopleService.CreateAsync(newPerson);
    }

    public static string CreateRandomName()
    {
        ...
    }
}
```

The `Observe` method in the model code subscribes the `People` state to the messenger's entity-change messages (`EntityMessage<Person>`).

An `EntityMessage<T>` carries an `EntityChange` enum value which indicates its type of change (`Created`, `Updated`, and `Deleted`) and the entity changed.

These messages are sent in the service upon successful creation of a `Person`, signaling the model to update itself with the new data. This is automatically reflected in the `People` `ListState`, which adds the newly created `Person`.

The service's code looks like the following:

```csharp
public class PeopleService : IPeopleService
{
    protected IPeopleRepository PeopleRepository { get; }
    protected IMessenger Messenger { get; }

    public PeopleService(IPeopleRepository peopleRepository, IMessenger messenger)
    {
        PeopleRepository = peopleRepository;
        Messenger = messenger;
    }

    public async ValueTask<IImmutableList<Person>> GetAllAsync(CancellationToken ct)
    {
        var allPeople = await PeopleRepository.GetAllPeople(ct);

        return allPeople.ToImmutableList();
    }

    public async ValueTask CreateAsync(Person person, CancellationToken ct)
    {
        var createdPerson = await PeopleRepository.CreateAsync(person, ct);

        Messenger.Send(new EntityMessage<Person>(EntityChange.Created, createdPerson));
    }
}
```

As you can see, the messenger's `Send` method is called in the `CreateAsync` call, specifying a 'created' `EntityMessage` along with the newly created person. The model observes this type of message `EntityMessage<Person>`, and as the `People` list-state has been subscribed to this, it will be updated with the newly created person added to it.

#### Additional Observe overloads

The `Observe` extension method comes in several flavors.
They all share a common goal - to send and intercept entity-message messages to and from the messenger and apply them to a state or feed-state. They also share a `keySelector` parameter which uses to determine by which property the entity is identified. This is important so that the state can compare or look up an appropriate entity where needed; for example, when an entity is updated, it will replace the old one with the new one received with the entity message.

- `Observe<TEntity, TKey>(IState<TEntity> state, Func<TEntity, TKey> keySelector)`

    This overload takes the state to apply the entity messages on and the key-selector that determines by which property this entity is identified.
    It is designated for single-value states (`IState<T>`) and will update the state if an entity message has been received about an update or a deletion of an entity that is equal to the one currently present in the state.

    In the following example, if any of the details of the current user change and an update entity message has been sent with the messenger about the change, the `CurrentUser` state will update the `User` entity to reflect those changes.

    ```csharp
    public partial record MyModel
    {
        protected IUserService UserService { get; }

        public MyModel(IUserService userService)
        {
            UserService = userService;

            messenger.Observe(CurrentUser, user => user.Id);
        }

        public IState<User> CurrentUser => State.Async(this, UserService.GetCurrentUser);
    }
    ```

    The same applies to a deleted entity - the state will clear the current value.

- `Observe<TEntity, TKey>(IListState<TEntity> listState, Func<TEntity, TKey> keySelector)`

    This overload is similar to the previous one, except it takes a list-state and applies the entity messages on it, using the key-selector parameter to determine by which property this entity is identified with for comparison purposes.
    As long as entity-messages are sent from the service, the list-state will remain in sync and reflect newly added, deleted, or updated entities.

    This method is also the one that was used in the sample code above:

    ```csharp
    public partial record PeopleModel
    {
        protected IPeopleService PeopleService { get; }

        public PeopleModel(IPeopleService peopleService, IMessenger messenger)
        {
            PeopleService = peopleService;

            messenger.Observe(state: People, keySelector: person => person.Name);
        }

        public IListState<Person> People => ListState.Async(this, PeopleService.GetAllAsync);
    }
    ```

- `Observe<TOther, TEntity, TKey>(IListState<TEntity> listState, IFeed<TOther> other, Func<TOther, TEntity, bool> predicate, Func<TEntity, TKey> keySelector)`

This overload intercepts entity-change messages from the messenger for a specific entity type but only refreshes the state when the predicate returns `true` based on related entities from another feed.

Using the previous example, if each `Person` has a list of `Phone` with a `Phone.PersonId` property associating them to their owning `Person`, making changes to a `Phone` (e.g., removing one), will have the service send an entity-change message which will refresh the `SelectedPersonPhones` list-state, but only if the `Phone.PersonId` matches with the currently selected person `Id`:

```csharp
public partial record PeopleModel
{
    protected IPeopleService PeopleService { get; }
    protected IPhoneService PhoneService { get; }

    public PeopleModel(IPeopleService peopleService, IPhoneService phoneService, IMessenger messenger)
    {
        PeopleService = peopleService;
        PhoneService = phoneService;

        messenger.Observe(People, person => person.Id);

        messenger.Observe(
            SelectedPersonPhones,
            SelectedPerson,
            (person, phones) => true,
            person => person.Id);
    }

    public IListState<Person> People =>
        ListState
        .Async(this, PeopleService.GetPeople)
        .Selection(SelectedPerson);

    public IState<Person> NewPerson => State<Person>.Value(this, Person.EmptyPerson);

    public IState<Person> SelectedPerson => State<Person>.Empty(this);

    public IListState<Phone> SelectedPersonPhones =>
        ListState.FromFeed(this, SelectedPerson.SelectAsync(GetAllPhonesSafe).AsListFeed());

    private async ValueTask<IImmutableList<Phone>> GetAllPhonesSafe(Person selectedPerson, CancellationToken ct)
    {
        if (selectedPerson == null)
            return ImmutableList<Phone>.Empty;

        return await PhoneService.GetAllPhones(selectedPerson, ct);
    }

    public async ValueTask AddPerson(CancellationToken ct = default)
    {
        var newPerson = (await NewPerson)!;

        await PeopleService.AddPerson(newPerson, ct);

        await NewPerson.Update(old => Person.EmptyPerson(), ct);
    }

    public async ValueTask RemovePhone(int phoneId, CancellationToken ct)
    {
        await PhoneService.DeletePhoneAsync(phoneId, ct);
    }
}
```

The `SelectedPersonPhone` state will only be refreshed if it meets the predicate criteria, which are limited to the currently selected `Person`.

> [!NOTE]
> The `Selection` method above picks up UI selection changes and reflects them onto a state. This subject is covered [here](xref:Uno.Extensions.Mvux.Advanced.Selection).

- `Observe<TOther, TEntity, TKey>(IState<TEntity> state, IFeed<TOther> other, Func<TOther, TEntity, bool> predicate, Func<TEntity, TKey> keySelector)`

This overload is the same as the previous one, except it watches a single-item state rather than a `ListState`, as in the last example.

#### Fluent API Observe overloads

These extension methods are available for both `IState<TEntity>` and `IListState<TEntity>` objects.

- `IState<TEntity> Observe<TEntity, TKey>(this IState<TEntity> state, IMessenger messenger, Func<TEntity, TKey> keySelector)`

    This overload is a fluent API extension method that can be used to observe entity-change messages from the messenger for a specific entity type and apply them to the state. It returns the state itself, so it can be chained with other methods.

    ```csharp
    public partial record MyModel
    {
        protected IUserService UserService { get; }

        public MyModel(IUserService userService, IMessenger messenger)
        {
            UserService = userService;

            CurrentUser
                .Observe(messenger, user => user.Id)
                .Observe(messenger, user => user.Name);
        }

        public IState<User> CurrentUser => State.Async(this, UserService.GetCurrentUser);
    }
    ```

    or in a more Fluent API way:

    ```csharp
    public partial record MyModel(IUserService UserService, IMessenger Messenger)
    {
        public IState<User> CurrentUser => State.Async(this, UserService.GetCurrentUser)
            .Observe(Messenger, user => user.Id)
            .Observe(Messenger, user => user.Name);
    }
    ```

    > [!NOTE]
    > Please note that in this example we are using C# Primary Constructors, which is a feature available in C# 9.0.

- `IState<TEntity> Observe<TEntity, TKey>(this IState<TEntity> state, IMessenger messenger, Func<TEntity, TKey> keySelector, out IDisposable disposable)`

    This overload is the same as the previous one, except it returns an `IDisposable`, which can be used to dispose of the subscription. When disposed, the state will stop observing further entity-change messages from the messenger.

    ```csharp
    public partial record MyModel
    {
        protected IUserService UserService { get; }
        private IDisposable subscriptions;

        public MyModel(IUserService userService, IMessenger messenger)
        {
            UserService = userService;

            CurrentUser
                .Observe(messenger, user => user.Id, out subscriptions);
        }

        public IState<User> CurrentUser => State.Async(this, UserService.GetCurrentUser);

        // Call this method to cancel the subscription
        private void CancelSubscriptions()
        {
            subscriptions.Dispose();
        }
    }
    ```

Two more overload extensions are available for `IListState<TEntity>`, and they behave the same as the `IState<TEntity>` overloads.

- `IListState<TEntity> Observe<TEntity, TKey>(this IListState<TEntity> listState, IMessenger messenger, Func<TEntity, TKey> keySelector, out IDisposable disposable)`

- `IListState<TEntity> Observe<TEntity, TKey>(this IListState<TEntity> listState, IMessenger messenger, Func<TEntity, TKey> keySelector)`

### Update

The MVUX messaging package also includes a pair of `Update` methods that enable updating an `IState<T>` or an `IListState<T>` from an `EntityMessage<T>`.
These messages' primary purpose is to serve the aforementioned `Observe` extension methods. However, they can also be used to create additional implementations of these methods.

These methods apply data from an `EntityMessage<T>` to an `IState<T>` or an `IListState<T>`. So, for example, if an entity-message contains a `created` `T` entity, applying this entity-message to a `ListState` will add that record to the `ListState`. The same applies to updating or removing.
