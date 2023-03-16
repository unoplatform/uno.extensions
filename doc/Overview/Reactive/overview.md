---
uid: Overview.Reactive.Overview
---

# MVUX Overview

## What is MVUX

MVUX stands for **M**odel-**V**iew-**U**pdate e**X**tended.

In MVU the **model** represents the state of the application and is passed into the **view** function. Input from the user triggers an **update** to the model. This is also referred to as the [Elm Architecture](https://en.wikipedia.org/wiki/Elm_(programming_language)#The_Elm_Architecture).  
MVUX **extend**s MVU with a powerful toolset that makes it possible to define the state of the application using immutable models (instead of mutable viewmodels used in an MVVM style application) whilst still leveraging the data binding capabilities of the Uno Platform.

It consists of four central components:

- [Model](#Model)
- [View](#View)
- [Update](#Update)
- [Extended](#Extended)

### Model

In this architecture the application state is represented by a model, which is updated by messages sent by the user in the view.
The view is in charge of rendering the current state of the model, while any input from the user updates the model and recreates it.
The Model in MVUx is what's known as 'View Model' in other architectures.

MVUx promotes immutability of data entities. Changes to the data are applied only via update messages sent across from the view to the model,
the model responds by performing the updates and the view reflects those changes.

Immutable entities makes raising change notification redundant, enables easier object equality comparison as well as other advantages.
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

MVUx wraps the asynchronous data request with metadata that tells us if the request is still in progress, has failed, or succeeded,
and if succeeded, whether the request contains any data entries.

#### Code generation

MVUx comes with a powerful code-generation engine that supplements the user with generated boilerplate code
that consists of model and entity proxy classes containing important properties and asynchronous commands.
The proxy-classes assist the UI with displaying the data according to its current state provided with the metadata.

#### UI Controls
MVUx also provides a set of UI tools that are specially tailored to automatically read and display that metadata,
providing templates for the various states, such as no data, error, progress tracking,
as well interactions with the server to enable easy refreshing and saving of the data.

#### Binding engine

It also comes with a powerful binding engine that reads the re-created model and updates the view accordingly.

## MVUx building blocks

The most important components in an MVUx app is either a feed (`IFeed<T>`), which is used for read-only scenarios,
or a state (`IState<T>`) which should be used when the user can apply changes to the data to be sent back to the model.

There is also a list flavor of the above components (`IListFeed<T>`/`IListState<T>` respectively).
The list version of feed and state offer additional operators for working with collections.

Here's a sample model exposing a feed:

```c#
public IFeed<Product[]> Products = Feed.Async(_productService.GetProducts);
```

For the view side, a special `FeedView` control is used, it's especially designed to interact with the feed and its metadata.  

```xaml
<Page 
	x:Class="MyProject.MyPage"
	xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	xmlns:mvux="using:Uno.Extensions.Reactive.UI">

<mvux:FeedView Source="{Binding Products}">
	<DataTemplate>
		<ListView ItemsSource="{Binding Data}" />
	</DataTemplate>

    <!-- optional -->
    <mvux:FeedView.ProgressTemplate>
        <DataTemplate>
          <TextBlock Text="Loading..." />
        </DataTemplate>
    </mvux:FeedView.ProgressTemplate>

    <mvux:FeedView.LoadingTemplate>

</mvux:FeedView>
```

MVUx also generates code which serves the `FeedView` with helper methods and commands to enable easy refreshing of data,
as well as propagating data-update messages back to the model.
