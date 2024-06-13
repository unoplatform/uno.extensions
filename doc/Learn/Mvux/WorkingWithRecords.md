---
uid: Uno.Extensions.Mvux.Records
---

# How to write Records with MVUX

## What is a Record

A [record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record) behaves like a class*, offering the feature of **immutability**, where the values assigned to it remain unchanged once set. It's possible to create records using the `record` modifier, for example:

```csharp
public record MyRecord();
```

Records **can** be, but are not necessarily, immutable. When creating a record, it's important to use the keyword `init` instead of `set` for properties. If you choose `set`, the record won't remain immutable. Consequently, you won't be able to prevent values from changing once they're set. Additionally, ensure that sub properties or objects also remain immutable to maintain the integrity of the entire structure.

Also, records introduce the `with` operator, which is a helpful tool to deal with immutable objects, we will see more about this operator in the **Updating records** section.

> [!IMPORTANT]
> \* When using `record struct`, there are some differences in how it behaves compared to regular records or classes because it combines value-type characteristics with the features of records. Learn more about [`struct`](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct).

## Why Immutability in MVUX

Immutability is crucial in MVUX for two main reasons:

### Predictable State Changes

Immutability ensures that once we set a state, it can't be changed. This predictability makes it easier to understand how our application behaves over time. In MVUX, we use immutable data structures to represent the application state.

### Concurrency and Threading

Immutability makes our application more robust in handling concurrency and threading challenges. Immutable data structures are naturally thread-safe, reducing the risk of bugs related to multiple things happening simultaneously in our app.

## How to create immutable records

You can create immutable records in three ways. First, declare your record with a [primary constructor](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/instance-constructors#primary-constructors) and parameters; this will create an immutable record with the specified parameters as its properties:

```csharp
public partial record ChatResponse(string Message, bool IsError);
```

The second way is by creating properties using the `init` keyword instead of `set` to enforce immutability. Here's a brief example:

```csharp
public partial record ChatResponse
{
    public string Message { get; init; }
    public bool IsError { get; init; }
}
```

Finally, it's possible to mix the two approaches by using the primary constructor and adding properties for non-essential values. For example, consider a `ChatResponse` record where `Message` is essential, but `IsError` is not:

```csharp
public partial record ChatResponse(string Message)
{
    public bool IsError { get; init; }
}
```

```csharp
new ChatResponse("Hello, I'm a bot"); //with IsError defaulting to false
```

## How to use records with MVUX

Records are designed to be a simple data structure, excellent for exchanging data, such as requests and responses, between application layers.

For instance, in our `ChatService`, the `AskAsync` method is called from the Model. It receives a list of `ChatEntry` records, which are used to create a request. The method returns a `ChatResponse` (record) instance to the Model, handling data from the presentation layer to the business layer:

```csharp
public async ValueTask<ChatResponse> AskAsync(IImmutableList<ChatEntry> history)
{
    var request = CreateRequest(history);

    var result = await _client.CreateCompletion(request);

    if (result.Successful)
    {
        var response = result.Choices.Select(choice => choice.Message.Content);

        return new ChatResponse(string.Join("", response));
    }
    else
    {
        return new ChatResponse(result.Error?.Message, IsError: true);
    }
}
```

## Updating records

As we are dealing with immutable records, it's not possible to update them or their properties. To achieve that, we need to create a new instance based on the previous record. The `with` operator will create a new instance of the object and do a **shallow** copy of all the members of the original object. This ensures we are not modifying data from the UI in the wrong thread. See the example:

Given the `Message` record:

```csharp
public partial record Message(string Content, Status status, Source source);
```

In our Model:

```csharp
...

message = message with
{
    Content = response.Message,
    Status = response.IsError ? Status.Error : Status.Value
};

//Then you can update your message list displayed in the UI, thread-safe
await Messages.UpdateAsync(message);
...

```

## App Examples

Check out our SimpleCalc workshop and ChatGPT sample to see how we put these tips into action in real apps, using MVUX and immutable records.

- [SimpleCalc Workshop](xref:Workshop.SimpleCalc.GettingStarted)
  - [MVUX & XAML](xref:Workshop.SimpleCalc.MVUX.XAML.FirstProject)
  - [MVUX & C# Markup](xref:Workshop.SimpleCalc.MVUX.CSharp.FirstProject)
- [ChatGPT Sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/ChatGPT)
