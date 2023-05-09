---
uid: Overview.Serialization
---

# Serialization

Accessing the serialized and deserialized representation of an object can be important for dynamic, data-rich applications. `Uno.Extensions.Serialization` allows for simplified access to serializer objects as dependencies. This library supports the new serialization [technique](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator) powered by code generation.

## Using a Serializer

The `ISerializer` interface provides a simple API for serializing and deserializing objects. Once serialization is enabled for an application, the `ISerializer` can be accessed as a dependency. The following example shows how to enable serialization for an application with the `UseSerialization()` extension method.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseSerialization()
        });
...
```

This extension method registers the `ISerializer` interface as a singleton service. The `ISerializer` interface can then be accessed as a dependency in any class that is registered with the host to serialize or deserialize objects.

```csharp
public class MyViewModel : ObservableObject
{
    private ISerializer _serializer;

    public MyViewModel(ISerializer serializer)
    {
        this._serializer = serializer;
    }

    public void Serialize()
    {
        var myObject = new MyObject();
        var json = _serializer.Serialize(myObject);
    }
}
```

## Configuring ISerializer

The default serializer implementation uses `System.Text.Json`. The serialization can be configured by registering an instance of `JsonSerializerOptions` with the host. The following example shows how to register an instance of `JsonSerializerOptions` with the host.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseSerialization(services =>
            {
                services
                    .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            });
        });
...
```

## See also

- [New serialization technique](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator)
- [System.Text.Json](https://learn.microsoft.com/dotnet/api/system.text.json)
- [Configuring the serializer](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions#properties)