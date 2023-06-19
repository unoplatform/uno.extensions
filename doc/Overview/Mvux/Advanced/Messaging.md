---
uid: Overview.Mvux.Advanced.Messaging
---

# Messaging

## Community Toolkit messenger

The Community Toolkit messenger can be used as a central tool to send and receive global messages between objects in the app. The advantage of the messenger is that objects can remain decoupled from each other as the messenger enables sending the messeges around without keeping a strong reference between the sender and the receiver. The messages can also be sent over specific channels uniquely identified by a token or within certain areas of the application.

MVUX includes extension methods that enable the integration between the [Community Toolkit messenger](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/messenger) and [MVUX feeds](xref:Overview.Mvux.Feeds). But before discussing how MVUX integrates with the Community Toolkit messenger, let's have a quick look on how the messenger works.

The core component of the messenger is the `IMessenger` object. Its main methods are `Register` and `Send`. `Register` subscribes an object to start listening to messages of a certain type, whereas `Send` sends out messages to all listening parties.  
There are various ways to obtain the `IMessenger` object but we'll use the most common one, which involves using [Dependency Injection](xref:Overview.DependencyInjection) (DI) to register the `IMessenger` service in the app so it can then be resolved at the construction of other dependent types (e.g. ViewModels).

In the model, we obtain a reference to the `IMessenger` on the constructor, which is resolved by the DI's service provider.
The `Register` method has quite a few overloads, but for the sake of this example let's use the one that takes the recipient and the message type as parameters. The first parameter is the recipient (`this` in this case), the second one is a callback that is executed when a message has been received. Although `this` can be called within the callback, it's prefered that the callback doesn't make external references and that the `MyModel` is passed in as an argument.  
`MessageReceived` is then called on the received recipient (which is the current `MyModel`), and the message is passed into it:

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

Then, in another location or module in the app, a `MyMessage` is sent:

```csharp
using CommunityToolkit.Mvvm.Messaging;

public partial record AnotherModel
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

MVUX harnesses the power of the Community Toolkit messenger and adds extension methods that enable you to listen to entity changes received from the messenger and have them automatically applied to the state or list-state storing the entities in the current model. The following entity-change types are supported: created, updated, deleted.

For instance, when there is a command in the model that when executed creates a new entity and stores it to the database using a service, the service can send a 'created' entity-change message with the messenger, which can then be intercepted in the model to have the state or list-state update itself and display the newly created entity received from the messenger.

### Observe

In the example below, there is a model that displays a state-list of `Person` entities received from a service, loaded using a state, with the [`Async`](xref:Overview.Mvux.ListStates#async) factory-method.

As you can gather from the code, the service interacts with an external source to load and save `Person` data. In the example below we can see the use of two of its methods: `GetAllAsync`, and `CreateNameAsync`.

There's also a method `CreateNewPerson` which gets generated as a command in the bindable proxy, to be invoked from the View (refer to [commands](xref:Overview.Mvux.Advanced.Commands) to learn about how MVUX generates commands). This method uses `CreateRandomName`, which generates a random name. Its implementation has been removed for brevity.

The line using the MVUX messaging extension method, is the one calling `messenger.Observe`. Read the code and this line will be explained thereafter.

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

The `Observe` method in the model code, subscribes the `People` state to the messenger's entity-change messages (`EntityMessage<Person>`).

An `EntityMessage<T>` carries an `EntityChange` enum value which indicates its type of change (`Created`, `Updated`, and `Deleted`), as well as the actual entity that was changed.

These messages are sent in the service upon a successful creation of a `Person`, signaling the model to update itself with the new data. This is indeed automatically reflected in the `People` list-state which adds the newly created `Person`.

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

### Update
