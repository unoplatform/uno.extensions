---
uid: Overview.Reactive.FeedState
---

# Feeds & States

## Feeds wrap asynchronous data requests with metadata

When asynchronously requesting data from a service, we go through several states.

One state is about the actual request, if it's still in operation or not (Progress),
and another is, when we already have a result.
However the result can of several options as well:

- **Error**: the request resulted in an error.
- **None**: the data returned from the service contained no entities.
- **Some**: valid data has been returned.

Feeds are used to display data requested asynchronously from a service,
and wrap the data with additional metadata telling more about the current state of the data.



## States are feeds 

The only difference is that states are stateful, so that when the user changes the data in the UI,
MVUX's binding-engine sends update message to the Model which refreshes the state.
