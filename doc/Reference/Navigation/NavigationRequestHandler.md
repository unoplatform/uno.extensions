---
uid: Reference.Navigation.RequestHandler
---

# Building a Custom Request Handler with `IRequestHandler`

`IRequestHandler` plays a vital role in linking UI controls to navigation requests. This interface allows you to define how a view should respond to a navigation action and helps in handling the routing or navigation logic within a control.

This guide will explain how to create a custom `IRequestHandler` for your own controls, providing examples and best practices to ensure correct implementation.

## What is `IRequestHandler`?

The `IRequestHandler` interface defines two essential methods:

- **`CanBind(FrameworkElement view)`**: This method determines whether the handler can be bound to the given view.
- **`Bind(FrameworkElement view)`**: This method binds the handler to the view, attaching any necessary logic or event listeners.

```csharp
public interface IRequestHandler
{
    bool CanBind(FrameworkElement view);
    IRequestBinding? Bind(FrameworkElement view);
}
```

These methods allow you to specify how the handler interacts with the control and how it should respond to navigation requests.

## Steps to Create a Custom `IRequestHandler`

### 1. Implement `IRequestHandler`

Start by creating a class that implements the `IRequestHandler` interface. The key part here is to define the specific logic for the control that you want to bind.

Alternatively you could extend one of the provided base classes, depending on your needs:

- `ControlRequestHandlerBase<TControl>`
  This base class is useful when you are binding to a specific control type. It already implements `IRequestHandler` and simplifies the process by letting you focus on control-specific logic.

  ```csharp
  public abstract record ControlRequestHandlerBase<TControl>(ILogger Logger) : IRequestHandler;
  ```

- `ActionRequestHandlerBase<TView>`
  If your control needs to handle callback actions when subscribing and unsubscribing to events (such as click events), you should extend `ActionRequestHandlerBase`. This base class adds the necessary infrastructure for subscribing/unsubscribing from control-specific events.

  ```csharp
  public abstract record ActionRequestHandlerBase<TView>(ILogger Logger, IRouteResolver Resolver) : ControlRequestHandlerBase<TView>(Logger)
    where TView : FrameworkElement;
  ```

### 2. Check if the Control Can Be Bound (`CanBind`)

> [!NOTE]
> If you are extending `ControlRequestHandlerBase<TControl>` or `ActionRequestHandlerBase<TView>` the `CanBind` method is already implemented. You can check its implementation [here](https://github.com/unoplatform/uno.extensions/blob/d4fa6e44326bf140d08fb1eb205d4acba1ffe202/src/Uno.Extensions.Navigation.UI/Controls/ControlRequestHandlerBase.cs#L14-L37).

In the `CanBind` method, verify that the control you are working with is the correct type. For instance, if you are creating a handler for a custom control named `MyCustomControl`, the `CanBind` method should return `true` only if the `FrameworkElement` is of that type.

Example:

```csharp
public bool CanBind(FrameworkElement view)
{
    return view is MyCustomControl;
}
```

This method ensures that your handler will only attempt to bind to appropriate controls.

### 3. Bind the Control to Navigation (`Bind`)

The `Bind` method is where the actual magic happens. Here, you attach event handlers or other logic to respond to the control's events, such as clicks, pointer interactions, or selection changes.

### 4. Manage Resource Cleanup and Unbinding

Ensure that the events are detached properly when the control is unloaded to avoid memory leaks. Your custom `IRequestBinding` should take care of unsubscribing from events when no longer needed.

In the `Bind` method, you should return a `RequestBinding` object that takes care of attaching and detaching event handlers as the control is loaded and unloaded.

### 5. Register Your Custom `IRequestHandler`

After implementing the `IRequestHandler`, you need to register it in your application. Typically, this is done in your service registration:

```csharp
services.AddSingleton<IRequestHandler, MyCustomControlRequestHandler>();
```

This allows your custom handler to be automatically picked up and used whenever a navigation request involves `MyCustomControl`.
