---
uid: Uno.Extensions.Serialization.Overview
---

# Serialization

**Serialization** involves converting an object into a format that can be easily stored or transmitted. On the receiving end, the object is reconstructed back into its original form through **deserialization**. These two operations complement each other and can be important for dynamic, data-rich applications.

`Uno.Extensions.Serialization` allows for simplified access to serializer objects as dependencies. This library supports the new serialization [technique](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator) powered by code generation.

## Installation

`Serialization` is provided as an Uno Feature. To enable `Serialization` support in your application, add `Serialization` to the `<UnoFeatures>` property in the Class Library (.csproj) file.

[!include[existing-app](../includes/existing-app.md)]

[!include[single-project](../includes/single-project.md)]

For more information about `UnoFeatures` refer to our [Using the Uno.Sdk](xref:Uno.Features.Uno.Sdk) docs.

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
        var myObject = new Person();
        var json = _serializer.Serialize(myObject);
    }
}
```

## Configuring ISerializer

### JSON

The default serializer implementation only supports serializing to JSON. Because it uses `System.Text.Json`, the specifics of this behavior can be configured with `JsonSerializerOptions`. The following example shows how to register an instance of `JsonSerializerOptions` with the host.

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

#### Source Generation

As of .NET 6, a code generation-enabled serializer is supported. The type to serialize is named `Person` in this example.

```csharp
public record Person(string name, int age, double height, double weight);
```

To leverage the source generation feature for JSON serialization in an Uno.Extensions application, define a partial class which derives from a `JsonSerializerContext`, and specify which type is serializable with the `JsonSerializable` attribute:

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Person))]
public partial class PersonJsonContext : JsonSerializerContext
{
}
```

This partial class can then be registered with the host using the `AddJsonTypeInfo` extension method:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseSerialization(services =>
            {
                services
                    .AddJsonTypeInfo(PersonJsonContext.Default.Person)
            });
        });
...
```

It follows that the `PersonJsonContext` will be automatically used by the injected `ISerializer` to serialize and deserialize instances of `Person`:

```csharp
Person person = new("Megan", 23, 1.8, 90.0);

var json = _serializer.ToString<Person>(person);
```

### Custom

The `ISerializer` interface can be implemented to support custom serialization to formats like XML or binary. This example shows how to register a custom `ISerializer` based type named `XmlSerializerImpl` with the host.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseSerialization(services =>
            {
                services
                    .AddSingleton<ISerializer, XmlSerializerImpl>();
            });
        });
...
```

## See also

- [New serialization technique](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator)
- [System.Text.Json](https://learn.microsoft.com/dotnet/api/system.text.json)
- [Configuring the serializer](https://learn.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions#properties)
