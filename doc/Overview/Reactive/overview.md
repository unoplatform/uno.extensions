---
uid: Overview.Reactive.Overview
---

# MVUx Overview

## What is MVUx

MVUx stands for **M**odel-**V**iew-**U**pdate E**x**tended.

It utilizes a flow where the **model** is rendered on the **view** side, and once changed by request from user, gets **update**d,
also know as [Elm Architecture](https://en.wikipedia.org/wiki/Elm_(programming_language)#The_Elm_Architecture).  
Uno Platform's MVUx **extend**s this further by providing a powerful toolset, a code-generation and binding engine.  
MVUx enables the user to write the UI markup in an agnostic way, not necessarily XAML.

It consists of four central components:

- [Model](#Model)
- [View](#View)
- [Update](#Update)
- [Extended](#Extended)

### Model

In this architecture the application state is represented by a model, which is updated by messages sent by the user in the view.
The view is in charge of rendering the current state of the model, while any input from the user updates the model and recreates it.

MVUx promotes immutability of data entities. Changes to the data are applied only via update messages sent across,
which in turn recreates the model.

The advantage of keeping the model immutable is so that we don't have to worry about raising change notification,
it enables easier object equality comparison as well as other advantages.
The ideal type for creating immutable data-objects is
[record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) types.
They are immutable by nature and a perfect fit for working with feeds and MVU architecture.
Record types also feature the special
[`with` expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/with-expression),
which enables recreating the same model with a modified set of properties, while retaining immutability.


### View

The view is the UI layer displaying the current state of the model.  
When a user interacts with the view and request changes to the data,
and update message is sent back to the model that updates the data, which is then reflected back on the view.

### Update

Whenever a user makes any sort of input activity that affects the displayed model,
instead of specifically notifying changes for those specific properties that were changed,
a message is sent back to the model which responds by re-creating the model which the view then updates.  
It's important to note that the view is not being re-created, it rather just updates with the fresh model.

### Extended

MVUx extends the MVU model by harnessing powerful features that make it easier to track asynchronous requests to a remote server
or other long running data-sources, and display the resulting data appropriately.  

The key features Uno Platform provides in the MVUx toolbox are:

- [Metadata](#Metadata)
- [Code generation](#Code-Generation)
- [UI Controls](#UI-Controls)
- [Binding engine](#Binding-engine)

#### Metadata

The way MVUx achieves that is by keeping a feed of a data-source, and reporting with metadata about what's happening.
Among this metadata are details about the request itself, whether it's still in progress, succeeded, or failed;
and about the returned data, letting us know if there was any data present or the response contained no data.  
The metadata also holds the previous state of the data, in case anything goes wrong and we want to retain the old state.  
This information is then used to shape the UI accordingly, for example, if there are no data or if an error occurred.

The metadata currently supported by MVUx out the box is as follows:

* Data: the request has been responded to with a valid data response.
    + Some: there are data items
    + None: the server found no records for the query
* Loading (Y/N): telling if the feed is currently awaiting the data
* Error (Y/N): telling if an error occurred while requesting/awaiting the data

The metadata is built in an extensible way and more features are being reviewed and planned, for example validation errors.

If you're familiar with the `IObservable<T>` interface, a feed behaves in a quite similar manner except `IObservable<T>`
doesn't support this layer of metadata.

#### Code generation

In addition, MVUx reads the model generates boilerplate source code that makes it easier to access the data
and the metadata around the current state and its model, as well as asynchronous commands that can be hooked up to actions from the UI.

The generated code can be inspected by navigating to the project dependencies, selecting the currently active platform, then Analyzers,
under _Uno.Extensions.Reactive.Generator_:

<!-- TODO update link -->
![](https://i.imgur.com/w1sSFYG.png)


The code generation in MVUx addresses these important points:

- **Model**
  This is an extended mirror type of the model layer (aka ViewModel in other architectures).  
  It adds extended feed properties, regenerated command properties, and more.
- **Bindables**  
  For each entity exposed via a feed (a read-only data-feed, will be explained [soon](#Command)), a wrapper helper class is generated when necessary,
  to enable the binding engine to communicate these changes back to the model whenever there's new data available.
- **Feeds**  
  For each feed publicly declared in the model, an upgraded `IFeed<T>` is generated where `T` is a bindable of `T`,
  to accommodate for the binding engine.
- **Commands**
  When following conventions or if explicitly requested (using attributes),
  a special `ICommand` is generated, ready to be used asynchronously is generated.  
  Read more about commanding in WinUI apps [here](https://learn.microsoft.com/en-us/windows/apps/design/controls/commanding).

#### UI Controls
MVUx also provides a set of UI tools that are specially tailored to automatically read and display that metdata,
providing templates for the various states, such as no data, error, progress tracking,
as well interactions with the server to enable easy refreshing and saving of the data.

#### Binding engine

MVUx comes with a powerful binding engine that reads the re-created model and updates the view accordingly.
Whenever a change is submitted to one of the properties, the model is re-created and the feed is updated with the updated entity.
This also opens the UI layer to additional languages, not just XAML, C# markup for instance.

## MVUx building blocks

The following list gives an brief introduction to the main components of MVUx.

### Feed: read-only data

MVUx uses `IFeed<T>` (where `T` is the data type) as a placeholder of an asynchronous call to a server or long-running process
maintaining the progress of the request and it's outcome,
while preserving the previous data if anything goes wrong with the current request.  
An `IListFeed<T>` is a feed of a collection of `T`, with additional features that enable dealing with collections.

A feed is only used to display data received from the server and track the progress of requesting it,
but it does not support changing it by the user or sending updated data back to the server, this is what State is for.

<!-- TODO link to feed docs, how tos, api ref -->

### State: read-write data

An `IState<T>` is a superset of `IFeed<T>` which adds support for sending back update data to the model,
which can then apply the changes remotely.
It provides support for re-creating the model which is then picked up by the UI tools

Like in feeds, `IListState<T>`, is a state of a collection of `T`, with additional operators for working with collections.

<!-- TODO link to state docs, how tos, api ref -->

### FeedView

The `FeedView` is a UI control that serves feeds and states.  
It supports the above metadata out the box and enables using specific data-templates for each of the aforementioned states.

<!-- TODO link to FeedView docs, how tos, api ref -->

### Command

MVUx code-generation engine reads the presentation layer and for each qualifying method,
generates a command that can be invoked from the UI.  
It also enables passing in a parameter when additional information is necessary to process the command.  
When invoked, the command can be used to load/update/save data, as well as updating the data in the feeds/states.

<!-- TODO link to command docs, how tos, api ref -->

#### Message
A message is a data packet wrapper that consists of the metadata that tells us about the data attached
if it exists, empty, or if the request failed, and obviously, the data itself if any.  
A message also provides a reference to the previous piece of metadata (with its previous data),
in case anything goes wrong and we chose to display the old information.  
It can also provide detailed information about all changes made to the data.

The `Message.With()` method provides a functional way of building a message using a
[fluent-interface](https://en.wikipedia.org/wiki/Fluent_interface).

#### Option

An option is the inner layer of the data in a message and represents one of 3 options regarding the inner data:

- Some: represents valid data
- None: Indicates that a value has been loaded but should be considered empty, and we should not be rendered as-is in the UI.
In our example, when you cannot ship to the selected country.
- Undefined: This represents a missing value, i.e. there is no info about the data yet.
Typically this is because we are still loading it asynchronously.

#### Bindable proxy

A generated version of the entity the presentation layer provides (the one the feed or the state provide).  
This is to provide UI data-binding capabilities for the immutable entity to reflect all changes from the UI,
and to allow updates of the state back in the presentation layer.  
Bindable proxies also enable descended types property changes, such as `Person.Phone.Number`.
