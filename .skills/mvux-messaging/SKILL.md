---
name: mvux-messaging
description: Use messaging to sync MVUX states with entity changes. Use when implementing CRUD operations that update multiple views, decoupling services from models, or broadcasting entity changes.
metadata:
  author: uno-platform
  version: "1.0"
  category: uno-platform-mvux
---

# MVUX Messaging

MVUX messaging enables automatic synchronization between services and states using the CommunityToolkit.Mvvm messenger pattern.

## Prerequisites

- Add `MVUX` to `<UnoFeatures>` in your `.csproj`
- CommunityToolkit.Mvvm is included with MVUX

```xml
<UnoFeatures>MVUX</UnoFeatures>
```

## Why Use Messaging?

- **Decoupling:** Services don't need references to models
- **Multi-view sync:** All views showing the same data stay in sync
- **CRUD propagation:** Create/Update/Delete operations update all relevant states

## Register Messenger in DI

```csharp
using CommunityToolkit.Mvvm.Messaging;

// In App.xaml.cs or host configuration
builder.Services.AddSingleton<IMessenger, WeakReferenceMessenger>();
```

## Entity Change Messages

MVUX uses `EntityMessage<T>` to communicate entity changes:

```csharp
using Uno.Extensions.Reactive.Messaging;

public enum EntityChange
{
    Created,
    Updated,
    Deleted
}

// Sent by services when entities change
var message = new EntityMessage<Person>(EntityChange.Created, newPerson);
```

## Sending Entity Changes from Service

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
        => (await _repo.GetAllAsync(ct)).ToImmutableList();
    
    public async ValueTask CreateAsync(Person person, CancellationToken ct)
    {
        var created = await _repo.CreateAsync(person, ct);
        
        // Broadcast: a Person was created
        _messenger.Send(new EntityMessage<Person>(EntityChange.Created, created));
    }
    
    public async ValueTask UpdateAsync(Person person, CancellationToken ct)
    {
        var updated = await _repo.UpdateAsync(person, ct);
        
        // Broadcast: a Person was updated
        _messenger.Send(new EntityMessage<Person>(EntityChange.Updated, updated));
    }
    
    public async ValueTask DeleteAsync(string personId, CancellationToken ct)
    {
        var deleted = await _repo.DeleteAsync(personId, ct);
        
        // Broadcast: a Person was deleted
        _messenger.Send(new EntityMessage<Person>(EntityChange.Deleted, deleted));
    }
}
```

## Observing Entity Changes in Model (List)

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public partial record PeopleModel
{
    private readonly IPeopleService _service;
    
    public IListState<Person> People => 
        ListState.Async(this, _service.GetAllAsync);
    
    public PeopleModel(IPeopleService service, IMessenger messenger)
    {
        _service = service;
        
        // Subscribe to Person entity changes
        messenger.Observe(
            state: People,
            keySelector: person => person.Id);
    }
}
```

### What Happens

| EntityChange | Effect on ListState |
|--------------|---------------------|
| `Created` | Item added to list |
| `Updated` | Matching item replaced |
| `Deleted` | Matching item removed |

## Observing Entity Changes (Single State)

```csharp
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

public partial record ProfileModel
{
    private readonly IUserService _service;
    
    public IState<User> CurrentUser => 
        State.Async(this, _service.GetCurrentUserAsync);
    
    public ProfileModel(IUserService service, IMessenger messenger)
    {
        _service = service;
        
        // Subscribe to User entity changes
        CurrentUser.Observe(
            messenger,
            user => user.Id);
    }
}
```

### What Happens

| EntityChange | Effect on State |
|--------------|-----------------|
| `Updated` | State value replaced if ID matches |
| `Deleted` | State cleared if ID matches |

## Key Selector

The key selector determines how to match entities:

```csharp
// By ID (recommended)
messenger.Observe(People, person => person.Id);

// By composite key
messenger.Observe(Orders, order => (order.CustomerId, order.OrderId));

// By unique property
messenger.Observe(Users, user => user.Email);
```

**Important:** Use a stable, unique identifier. Avoid properties that can change.

## Disposing Subscriptions

```csharp
public partial record UserModel
{
    private readonly IDisposable _subscription;
    
    public IState<User> CurrentUser => ...;
    
    public UserModel(IUserService service, IMessenger messenger)
    {
        CurrentUser.Observe(messenger, user => user.Id, out _subscription);
    }
    
    // Call when done
    public void Dispose()
    {
        _subscription.Dispose();
    }
}
```

## Custom Messages

For non-entity messages:

```csharp
using CommunityToolkit.Mvvm.Messaging;

// Define message
public record RefreshDataMessage;

// Register in model
public partial record DashboardModel
{
    public DashboardModel(IMessenger messenger)
    {
        messenger.Register<DashboardModel, RefreshDataMessage>(
            this,
            static (recipient, message) => recipient.OnRefresh());
    }
    
    private void OnRefresh()
    {
        // Trigger refresh
    }
}

// Send from anywhere
messenger.Send(new RefreshDataMessage());
```

## Manual Entity Update

Apply entity message manually:

```csharp
using Uno.Extensions.Reactive.Messaging;

// For list state
public void ApplyChange(EntityMessage<Person> message)
{
    People.Update(message, person => person.Id);
}

// For single state
public void ApplyChange(EntityMessage<User> message)
{
    CurrentUser.Update(message, user => user.Id);
}
```

## Complete Example

### Service

```csharp
public partial record Contact(string Id, string Name, string Email);

public class ContactService : IContactService
{
    private readonly IContactRepository _repo;
    private readonly IMessenger _messenger;
    
    public ContactService(IContactRepository repo, IMessenger messenger)
    {
        _repo = repo;
        _messenger = messenger;
    }
    
    public async ValueTask<IImmutableList<Contact>> GetAllAsync(CancellationToken ct)
        => (await _repo.GetAllAsync(ct)).ToImmutableList();
    
    public async ValueTask<Contact> CreateAsync(Contact contact, CancellationToken ct)
    {
        var created = await _repo.CreateAsync(contact, ct);
        _messenger.Send(new EntityMessage<Contact>(EntityChange.Created, created));
        return created;
    }
    
    public async ValueTask<Contact> UpdateAsync(Contact contact, CancellationToken ct)
    {
        var updated = await _repo.UpdateAsync(contact, ct);
        _messenger.Send(new EntityMessage<Contact>(EntityChange.Updated, updated));
        return updated;
    }
    
    public async ValueTask DeleteAsync(string id, CancellationToken ct)
    {
        var deleted = await _repo.DeleteAsync(id, ct);
        _messenger.Send(new EntityMessage<Contact>(EntityChange.Deleted, deleted));
    }
}
```

### List Model

```csharp
public partial record ContactListModel
{
    private readonly IContactService _service;
    
    public IListState<Contact> Contacts => 
        ListState.Async(this, _service.GetAllAsync);
    
    public ContactListModel(IContactService service, IMessenger messenger)
    {
        _service = service;
        
        // Auto-sync with entity changes
        messenger.Observe(Contacts, c => c.Id);
    }
    
    public async ValueTask DeleteContact(Contact contact, CancellationToken ct)
    {
        // Service sends EntityMessage - list updates automatically
        await _service.DeleteAsync(contact.Id, ct);
    }
}
```

### Edit Model (Different Page)

```csharp
public partial record ContactEditModel
{
    private readonly IContactService _service;
    
    public IState<Contact?> Contact => State<Contact?>.Empty(this);
    
    public ContactEditModel(IContactService service, IMessenger messenger)
    {
        _service = service;
        
        // Sync with updates/deletes
        Contact.Observe(messenger, c => c.Id);
    }
    
    public async ValueTask SaveContact(CancellationToken ct)
    {
        var contact = await Contact;
        if (contact is not null)
        {
            // Service sends EntityMessage - all views update
            await _service.UpdateAsync(contact, ct);
        }
    }
}
```

## Best Practices

1. **Register IMessenger as singleton** in DI
2. **Use WeakReferenceMessenger** to prevent memory leaks
3. **Choose stable keys** (database IDs, not mutable properties)
4. **Send messages from services** not models
5. **Use Observe()** for automatic state synchronization
6. **Dispose subscriptions** when model is done
7. **Handle all entity changes** (Created, Updated, Deleted)
