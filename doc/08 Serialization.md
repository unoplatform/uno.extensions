## Serialization

Register serializer that implements `ISerializer`

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseSerialization()
        .Build();
    // ........ //
}
```

Access `ISerializer` to serialize and deserialize json data

## Configuring ISerializer

The default serializer implementation uses System.Text.Json. The serialization can be configured by registering an instance of JsonSerializerOptions

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseSerialization()
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        .Build();
    // ........ //
}
```
