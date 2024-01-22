---
uid: Uno.Extensions.Reactive.General
---
# General guidelines

## API

The main API provided by this package is [`IFeed<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/IFeed.cs) which represents a stream of _data_.

Unlike `IObservable<T>` or `IAsyncEnumerable<T>`, _feed_ streams are specialized to handle business data objects which are expected to be rendered by the UI.
A _feed_ is a sequence of _messages_ which contains immutable _data_ with metadata such as its loading progress and/or errors raised while loading it.
Everything is about _data_, which means that a _feed_ will never fail. It will instead report any error encountered with the data itself, remaining active for future updates.

Each [`Message<T>`](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Reactive/Core/Message.cs) contains the current and the previous state, as well as information about what changed.
This means that a message is self-sufficient to get the current data, but also gives enough information to update from the previous state in an optimized way without rebuilding everything.

## Good to know

* A basic _feed_ is **state-less**. The state is contained only in _messages_ that are going through a given subscription.
* The _data_ is expected to be immutable. An update of the _data_ implies a new _message_ with an updated instance.
* As in functional programming, _data_ is **optional**. A message may contain _data_ (a.k.a. `Some`), the information about the fact that there is no _data_ (a.k.a. `None`) or nothing at all, if for instance the loading failed (a.k.a. `Undefined`).
* Public _feeds_ are expected to be exposed in property getters. A caching system embedded in the reactive framework will then ensure to not re-create a new _feed_ on each get.
