---
uid: Reference.Reactive.Dev
---
# Feeds architecture

This document gives information about the architecture and the implementation of _feeds_. To get information about the usage off the _feeds_ framework, you should look [to this doc](xref:Uno.Extensions.Reactive.Concept).

## Dev general guidelines

* All instances of _feeds_ should be cached using the `AttachedProperty` helper.
* Consequently, all _feed_ implementation must be state less (except the special case of `State<T>`).
* When invoking a user asynchronous method, the `SourceContext` should be set as ambient (cf. `FeedHelper.InvokeAsync`).
* Untyped interfaces (`IMessage`, `IMessageEntry`, etc.) exists only for binding consideration and should not be re-implemented nor used in source code.

## Caching

In order to allow a light creation syntax in a property getter (`public Feed<int> MyFeed => _anotherFeed.Select(_ => 42)`, which is re-evaluated each time the property is get), but without rebuilding and re-querying the _feed_  each time, we have 2 levels of caching.

### Instance caching

First is the instance of the _feed_ itself. This is done using the `AttachedProperty` helper class. Each feed if attached to a `owner` and identified by a `key`. It makes sure that for a given `owner` we have only one instance of the declared _feed_, i.e. running `_anotherFeed.Select(_ => 42)` will always return the same instance.

The `owner` is usually (by order of preference):

1. The _parent feed_ if any (`_anotherFeed` in example above);
1. The [`Target`](https://learn.microsoft.com/dotnet/api/system.delegate.target) of the `key` delegate, so the instance of the class that is declaring the _feed_;
1. The `key` delegate itself if it’s a static delegate instance (e.g., in the example above `_ => 42` the `Target` is going to be `null` as we don’t have any capture)

The `key` is usually the delegate that **is provided by the user**. It’s really important here to note that a helper method like below would break the instance caching:

```csharp
public static Feed<string> ToString(this IFeed<T> feed, Func<T, string> toString)
  => feed.Select(t => toString(t));
```

As the delegate `t => toString(t)` is declared in the method itself, it will be re-instantiated each time. Valid implementations would have been:

```csharp
public static Feed<string> ToString(this IFeed<T> feed, Func<T, string> toString)
  => feed.Select(toString); // We are directly forwarding the user delegate
public static Feed<string> ToString(this IFeed<T> feed, Func<T, string> toString)
  => AttachedProperty.GetOrCreate(owner: feed, key: toString, factory: (theFeed, theToString) => theFeed.Select(t => theToString(t))); // We are explicitly caching the instance.
```

### Subscription caching

The second level of caching is for the “subscription” on a _feed_. This is needed to make sure that in a given _context_, enumerating / awaiting multiple times to a same _feed_ won’t re-build the value (noticeably, won’t re-request a value coming from a web API call).

This caching is achieved by the `SourceContext`.

Those _context_ are weakly attached to a owner (typically a `ViewModel`) and each call to `context.GetOrCreateSource(feed)` will return a state full subscription to the `feed` which will replay the last received _message_.

> When implementing an `IFeed`, the `context` provided in the `GetSource` is only intended to be used to restore it as current in some circumstances, like invoking a user’s async method.
  Your _feed_ must remain state less, so you should not use `context.GetOrCreateSource(parent)`.
>
> On the other side, each helper that allow user to “subscribe” to a _feed_ should do something like `SourceContext.Current.GetOrCreateSource(feed)` (and not `feed.GetSource(SourceContext.Current)`)

## Issuing messages

When implementing an `IFeed` you will have to create some messages.

If you don't have any _parent feed_ the easiest way is to start from `Message<T>.Initial` (do not send it as first message), then update it:

```csharp
var current = Message<int>.Initial;
for (var i = 0; i++; i < 42)
{
  yield return i;
  await Task.Delay(100, ct);
}
```

If you do have a _parent feed_, you should use the `MessageManager<TParent, TResult>`, eg.:

```csharp
var manager = new MessageManager<TParent, TResult>();
var msgIndex = 0;
await foreach(var parentMsg in _parent.GetSource(context))
{
  if (manager.Update(localMsg => localMsg.With(parentMsg).Data(msgIndex++)))
  {
    yield return manager.Current;
  }
}
```

> Make sure that your feed always produces at least on message. If there isn’t any relevant, send the `Message.Initial` before completing the `IAsyncEnumerable` source.
>
> If you have a _parent feed_ make sure to **always** forward the parent message, even if the parent message does not change any local value: `manager.Update(localMsg => localMsg.With(parentMsg))`.
>
> Be aware that enumeration of an `IAsyncEnumerable` is sequential (i.e. one value at once). The `MessageManager` also has a constructor that allows to asynchronously send messages to an `AsyncEnumerableSubject`.

## Axes

An _axe_ is referring to an “informational axe” related to a given _data_, a.k.a. a metadata. Currently the _feed_ framework is managing (i.e., actively generating value for) only 2 metadata: _error_ and _progress_, but as _Messages_ are designed to encapsulate a _data_ and all its metadata, a `MessageEntry` can have more than those 2 well-known axes.

```diagram
┌────┐1   *┌────────┐    2┌────────────┐1   *┌────────────────┐
│Feed├────►│Message │  ┌─►│MessageEntry├────►│MessageAxis     │
└────┘     ├────────┤  │  ├────────────┤     └─────┬──────────┘
           │Previous├──┤  │Data        │1          │1
           │Current ├──┘  │Error       ├──┐        │
           │Changes │     │Progress    │  │        │1
           └────────┘     └────────────┘  │ *┌─────┴──────────┐
                                          └─►│MessageAxisValue│
                                             └────────────────┘
```
<!-- To edit diagram: https://asciiflow.com/#/share/eJytUksKwjAQvcowazdVQexO%2FOyErlxlE3CoQk1hkkqLeAvxMOJpPInx3za2tGIYwiR58%2Ba9ITtUckPoqySKOhjJjBh93AlMBfrDfq8jMLNZdzC0maHU2IPAy%2BFcCg8AlHudD4sArx7SkrEqhLgVzoiW5bfjye5z0lqGBHdNXx6mynBWVzlK1%2FrmBt69vliFKnUvSL59g2jAWTMP%2BCx7ETBt13GiHUETaWQO5xWqPIdnnDCTMuDwTJlj%2FuCKJt6DK5GtpApJQ85rwHHIdubPI0CRwhX0v%2Fk9ev3%2BAaHxqvhgCxkl1JarlcWCYtzj%2FgrdASiH) -->

> `MessageEntry` are basically dictionaries of `MessageAxis` and `MessageAxisValue`, except **they are returning `MessageAxisValue.Unset` instead of throwing error for unset axes**.
>
> The `DataAxis` is a special axis that must be set on all entries. It exists only to unify/ease implementation. You should use `Option.Undefined` to “unset” the data.
>
> If you are about to add an axe, you should make sure to provided extensions methods over `IMessageBuilder` and `IMessageEntry` to read/write it directly to/from the effective metadata type. The generic `Get` and `Set` are there for that and should not be used directly in user’s code.

## Request

The subscriber of a feed can send some _request_ to the source feed to enhance its behavior.
The most common _request_ is the `RefreshRequest`.

> When implementing an `IFeed` you have access to those requests using the `Requests<TRequest>()` method on the `SourceContext` you get in the `GetSource`.
>
> When consuming a feed, you can send a request to that feed by creating a "child" context (`SourceContext.CreateChild()`) giving you own `IRequestSource`.

## Token and TokenSet

In a response to a _request_ a feed might issue a _token_ that is then added to its messages so the subscriber that  sent the request is able to track the completion of the _request_.
This is the case for the _Refresh_ and the _Pagination_ which are forwarding those tokens through the refresh and the pagintation axes.

As when a subscriber sends a _request_, it might be handled by more than one feed. For instance, when combining two `AsyncFeed` instances, the _refresh request_ will cause those two feeds to refresh their data.
Even if it's not yet the implemented, we can also imagine that an _operator feed_ (such as the `SelectAsyncFeed`) might also trigger a refresh of its own projection.
_Refresh_  and _Pagination_ axes are working with `TokenSet`. It is a collection of `IToken` that only keep the latest tokens for a given source in relation to the subscription context.

```diagram
        Subscriber       Combine     Async A     Async B

            │               │           │           │
       ┌─ Control channel (Requests on SourceContext) ─┐
       │    │               │           │           │  │
       │    │   Request 1   │           │           │  │
    ┌──┼──◄─┼──────────────►│ Request 1 │           │  │
    │  │    │               ├──────────►│           │  │
    ▼  │    │               │  Token A  │           │  │
    │  │    │               │◄──────────┘           │  │
    │  │    │               │       Request 1       │  │
    ▼  │    │               ├──────────────────────►│  │
    │  │    │               │        Token B        │  │
    │  │    │               │◄──────────┬───────────┤  │
    ▼  │    │ TokenSet[A,B] │           │           │  │
    │  │    │◄──────────────┤           │           │  │
    │  │    │               │           │           │  │
    ▼  └────┼───────────────┼───────────┼───────────┼──┘
IsExecuting │               │           │           │
    │  ┌─ Message channel ──┼───────────┼───────────┼──┐
    │  │    │               │           │           │  │
    ▼  │    │               ┤ Msg with  │           │  │
    │  │    │               │  token A  │           │  │
    │  │    │               │◄──────────┤ Msg with  │  │
    ▼  │    │   Msg with    │           │  token A  │  │
    │  │    │ TokenSet[A,B] │◄──────────┼───────────┤  │
    └─►├───►│◄──────────────┤           │           │  │
       │    │               │           │           │  │
       └────┼───────────────┼───────────┼───────────┼──┘
            │               │           │           │
```
<!-- To edit diagram: https://asciiflow.com/#/share/eJzNVs1OwzAMfhUrJ5B2AYEmelsnDhx2odwoh7ayumpdKppUdJp24xGm8S5oT8OTkK6BNV1Z%2BjexyGrtJLY%2Fx47bJaHOHIlBkzAckNBZYEwMsrRJahPj7uZ2YJOF4K6HQ8FxTLkQbAJyWInLvDhwMZYT42juBhR3%2FIgtqAejAm%2FaNhUEhfG13oI61JkDqT99IWSr44jyOArBmzqUYggXj%2FiaIOMMIgpWlMQeZltE6JdSRTEBzUaVev6SbuFKH4NiJse0p822PKOlnUoBgNahirwUn87Zp8b85rh5gKdohjQrrI44P961aDuYl1wxrW3C7YHy9LbALk%2FabH3CLWqxOguN6uOYdIj%2B3EjAe2D3KXoJD6jfvsHJc8rWJ8iY4%2BNvizsp%2BEYFVjdJP%2ByuIC3kz6OB%2BdLMkFI2de6%2BUoET5sNbwKddmw4A76l71bhbZdR%2FHod47rdWIlNRN0jQaXrAWn5H9pSLDZ3Vrh7QJPUfzJwbdf4xIyuy%2Bga89Mui) -->

> [!NOTE]
> As a subscriber, you can use the `TokenSetAwaiter` to wait for a set that has been produced by a request.
>
> [!NOTE]
> When implementing an `IFeed` you can use the `CoercingRequestManager` or the `SequentialRequestManager` to easily implement such request / token logic.
>
> When you answer to a request with a token, you **must** then issue a new message with that token.

## View

While developing in the _feed_ framework, the most interesting view-related part is the presentation layer. _Feeds_ are transferring _messages_ containing the data and its metadata, but those are not binding friendly. The presentation layer has then 2 responsibilities:

1. Read, i.e. from the ViewModel to the View, it will expose and maintain the state of the last message through standard binding-friendly properties and `INotifyPropertyChanged`;
1. Write, i.e. from the View to the ViewModel, it will convert back standard properties into so called `IInput`, which inherits from the `IFeed` interface, and which allows the ViewModel to manage the view just like any other data source.

Those binding friendly properties are generated by the `FeedsGenerator`.

> To trigger the generation, the class needs to have at least on constructor which has an `IInput`, or an `ICommandBuilder` parameter, or flag the type with `[ReactiveBindable(true)]` attribute.
>
> When we generate a binding friendly class for a type `MyType` we are generating a `BindableMyType` class nested into the `MyType` itself. This class inherits from the `BindableViewModelBase`.

### Denormalizing

In order to make a record editable field per field (e.g. a user profile where you want to edit first name and last name independently), we can generate a `BindableRecord` object that we re-expose each property of the `Record` independently.

> When a record is denormalized, we are generating / maintaining only one `State<Record>`. Setting a value of any of the denormalized properties, will update that root _state_.
>
> When a record is denormalized, we can no longer directly set a record instance on the `VM.Record` (the property is of type `BindableRecord`). To work around this, if the record does not have a `Value` property which would conflict, we are generating a get/set property `Value` which can be used to set the instance (e.g. `VM.Record.Value`).
>
> When a property is updated, we also ensure to raise the property changes, including for the `Value` property (if defined) and the `GetValue` method.

### Dispatching and threading considerations

The user’s `ViewModel` are expected to run on background thread for performance considerations. Commands and state updates from generated ViewModel classes are always ran on a background thread.

To avoid strong dependency on a dispatcher (neither by injection nor by the need to create VM on UI thread), all generated classes are auto resolving the dispatcher when an event handler is registered or when a public method is used, using the internal `LazyDispatcherResolver` and `EventManager`. Those classes are then completing their initialization once the first dispatcher is resolved.

The `EventManager` will capture the thread used to register an event handler and will then make sure to invoke that handler on that thread if it was a UI thread. This allows `BindableVM` to be data-bound to multiple window/UI threads.

```diagram
┌───────────────┐     ┌───────────────┐
│EventManager   │     │IInvocationList│
├───────────────┤1   *├───────────────┤
│Add(handler)   ├────►│Add(handler)   │
│Remove(handler)│     │Remove(handler)│
│Raise(args)    │     │Raise(args)    │
└───────────────┘     └───────▲───────┘
                              │
              ┌───────────────┴──────────────┐
              │                              │
  ┌───────────┴────────────┐    ┌────────────┴────────────┐
  │DispatcherInvocationList│    │MultiThreadInvocationList│
  └───────────▲────────────┘    └─────────────────────────┘
              │
        ┌─────┴───────┐
        │             │
  ┌─────┴─────┐   ┌───┴────┐
  │Coaslescing│   │Queueing│
  └───────────┘   └────────┘
```

<!-- To edit diagram: https://asciiflow.com/#/share/eJyrVspLzE1VslJyLUvNK%2FFNzEtMTy1S0lHKSawE0lZK1TFKFTFKVpYmJjoxSpVAlpG5JZBVklpRAuTEKD2asodUpAACZOiLickDksjuhJgDM8%2FTM68sPzmxJDM%2FzyezuASug1RkCDSOHH0Q2xxTUjQyEvNSclKLNLH4c9ourGogeoNSc%2FPLUuFSSH7DlIHqSMwsTtVILEov1lRADQ1MGfJCA39sTduEJzQU8AJsashwH6lRhGEhca6krjto4leYO10yiwsSS5IzUoswMgTUXt%2FSnJLMkIyi1MQUbHmGSLfhjnu6%2BRZ3eiLXLPQEQWL8E7QZZphzfmJxTmpxcmZeOlxbYGlqaSpEgLp2KtUq1QIAei9oAQ%3D%3D) -->

> We have one `IInvocationList` per UI thread and one for all background threads (the `MultiThreadInvocationList`).
>
> As a bonus, when a dispatcher bound handler is about to be invoked from another thread, it can be coalesced to be raised only once (cf. `CoalescingDispatcherInvocationList`).
