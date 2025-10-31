---
uid: Uno.Extensions.Serialization.Serialization.HowTo
title: Serialize JSON with Source Generators
tags: [serialization, json, system-text-json]
---
# Serialize JSON with source generators

Register Uno serialization with source-generated metadata so your apps can serialize and deserialize models efficiently using `System.Text.Json`.

## Enable the serialization feature

Add the `Serialization` feature to bring in `Uno.Extensions.Serialization`.

```diff
<UnoFeatures>
    Material;
    Extensions;
+   Serialization;
    Toolkit;
    MVUX;
</UnoFeatures>
```

## Register serialization with type metadata

Call `UseSerialization` during host configuration and add generated `JsonTypeInfo`.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host =>
        {
            host.UseSerialization(services =>
                services.AddJsonTypeInfo(PersonContext.Default.Person));
        });
}
```

`AddJsonTypeInfo` registers the compiled metadata so the serializer can avoid runtime reflection.

## Generate the context for your models

Create a partial `JsonSerializerContext` that lists each serializable type.

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Person))]
internal partial class PersonContext : JsonSerializerContext;

public sealed class Person
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public double Height { get; set; }
    public double Weight { get; set; }
}
```

At build time the source generator emits `PersonContext.Default.Person`, which exposes the required `JsonTypeInfo<Person>`.

## Customize serialization options

Register `JsonSerializerOptions` to tweak casing, naming, or converters.

```csharp
host.UseSerialization(services =>
{
    services.AddJsonTypeInfo(PersonContext.Default.Person);
    services.AddSingleton(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });
});
```

Any options registered in DI apply to all `ISerializer<T>` instances for `System.Text.Json`.

## Serialize and deserialize through DI

Inject `ISerializer<Person>` wherever you need to round-trip JSON.

```csharp
public class MainViewModel
{
    private readonly ISerializer<Person> _serializer;

    public MainViewModel(ISerializer<Person> serializer) => _serializer = serializer;

    public string Save(Person person) => _serializer.ToString(person);

    public Person Load(string json) => _serializer.FromString<Person>(json);
}
```

Uno provides synchronous methods (`ToString`, `FromString<T>`) and stream-based APIs when you need asynchronous I/O.

## Resources

- [Serialization overview](xref:Uno.Extensions.Serialization.Overview)
- [System.Text.Json source-generation docs](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-source-generation)
