---
uid: Uno.Extensions.Mvux.Records
---

# MVUX Records Primer

Quick reference for defining and using immutable records within MVUX models and services.

## TL;DR
- Prefer C# `record` types for MVUX models and DTOs to keep state immutable.
- Use `init` accessors or primary constructors; avoid mutable setters unless immutability is not required.
- Create new instances with the `with` expression when updating model data.

## Why Immutability Matters
- **Predictable updates**: MVUX models emit new snapshots rather than mutating shared state.
- **Thread safety**: immutable records can travel across dispatcher boundaries without race conditions.

## Creating Immutable Records
- Primary constructor:
  ```csharp
  public partial record ChatResponse(string Message, bool IsError);
  ```
- Property initializers with `init`:
  ```csharp
  public partial record ChatResponse
  {
      public string Message { get; init; }
      public bool IsError { get; init; }
  }
  ```
- Mix patterns for optional data:
  ```csharp
  public partial record ChatResponse(string Message)
  {
      public bool IsError { get; init; }
  }
  ```

## Using Records in MVUX Flows
- Treat records as DTOs between feeds, services, and states.
- Example service method that returns an immutable response:
  ```csharp
  public async ValueTask<ChatResponse> AskAsync(IImmutableList<ChatEntry> history)
  {
      var result = await _client.CreateCompletion(CreateRequest(history));
      return result.Successful
          ? new ChatResponse(string.Join("", result.Choices.Select(c => c.Message.Content)))
          : new ChatResponse(result.Error?.Message ?? "Unknown error") { IsError = true };
  }
  ```

## Updating Record Instances
- Records are immutable; clone with `with` when refreshing state:
  ```csharp
  message = message with
  {
      Content = response.Message,
      Status = response.IsError ? Status.Error : Status.Value
  };

  await Messages.UpdateAsync(message);
  ```
- `with` performs a shallow copy; ensure nested types are also immutable or cloned as needed.

## Related Samples
- SimpleCalc workshop (xref:Workshop.SimpleCalc.GettingStarted)
- ChatGPT sample: https://github.com/unoplatform/Uno.Samples/tree/master/UI/ChatGPT
- MVUX overview (xref:Uno.Extensions.Mvux.Overview)
