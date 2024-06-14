---
uid: Uno.Extensions.Reactive.Testing
---
# Test feed

In order to test your reactive application, you should install the `Uno.Extensions.Reactive.Testing` package in your test project.

Make your test class inherit from `FeedTests`, then in your tests methods, you can use the `.Record()` extensions method on the test you want to test.
It will subscribe to your feed and persist all received messages. Then you can assert the expected messages using the fluent assertions:

```csharp
[TestMethod]
public async Task When_ProviderReturnsValueSync_Then_GetSome()
{
    var sut = Feed.Async(async ct =>
    {
        await Task.Delay(500, ct);
        return 42;
    });
    using var result = await sut.Record();

    result.Should().Be(r => r
        .Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
        .Message(Changed.Data, 42, Error.No, Progress.Final)
    );
}
```

You define each axis (`Data` / `Error` / `Progress`) in the `Message` you want to validate. You can also define which axes are expected to have changed (`Changed`).

> [!NOTE]
> When developing a new _feed_, we recommend that you systematically validate all axes.
