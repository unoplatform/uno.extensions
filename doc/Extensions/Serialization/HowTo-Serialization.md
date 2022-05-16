# How-To: Serialize and Deserialize JSON Data

Accessing the serialized and deserialized representation of an object can be important for dynamic, data-rich applications. Uno.Extensions supports the [new](https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator) serialization technique powered by code generation, but you can optionally revert to the previous one which uses reflection.

## Step-by-steps

### 1. Opt into Serialization

* Call the `UseSerialization()` method to register a serializer that implements `ISerializer` with the service collection:

```csharp
    private IHost Host { get; }
    
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseSerialization( /* ..type info.. */)
            .Build();
        // ........ //
    }
```

### 2. Preparing the class to be serialized efficiently

* Below is a simple `Person` class which will be serialized to JSON:

```csharp
    internal class Person
    {
        public Person() { }

        public Person(string name, int age, double height, double weight)
        {
            Name = name;
            Age = age;
            Height = height;
            Weight = weight;
        }
        public string? Name { get; set; }
        
        public int Age { get; set; }
        public double  Height { get; set; }
        public double Weight { get; set; }
    }
```

* As of .NET 6, a code generation-enabled serializer is supported. To leverage this in your Uno.Extensions application, define a partial class which derives from a `JsonSerializerContext`, and specify which type is serializable with the `JsonSerializable` attribute:

```csharp
    using System.Text.Json.Serialization;
    
    [JsonSerializable(typeof(Person))]
    internal partial class PersonContext : JsonSerializerContext
    { }
```

* The `JsonSerializable` attribute will generate several new members on the `PersonContext` class, allowing you to access the `JsonTypeInfo` in a static context. Get the `JsonTypeInfo` for your class using `PersonContext.Default.Person` and add it within the serializer registration from above:

```csharp
    private IHost Host { get; }
    
    public App()
    {
        Host = UnoHost
            .CreateDefaultBuilder()
            .UseSerialization(services => 
                services
                    .AddJsonTypeInfo(PersonContext.Default.Person)
            )
            .Build();
        // ........ //
    }
```

### 3. Configuring the serializer

* The default serializer implementation uses `System.Text.Json`. The serialization can be configured by registering an instance of `JsonSerializerOptions`:

```csharp
private IHost Host { get; }

public App()
{
    Host = UnoHost
        .CreateDefaultBuilder()
        .UseSerialization(services => 
            services
                .AddJsonTypeInfo(PersonContext.Default.Person)
        )
        .ConfigureServices(services =>
        {
            services
                .AddSingleton(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        })
        .Build();
    // ........ //
}
```

### 4. Serialize and deserialize JSON data

* Add a constructor parameter for the `ISerializer<Person>` to a view model you registered:

```cs
    public class MainViewModel
    {
        private readonly ISerializer<Person> jsonSerializer;

        public MainViewModel(ISerializer<Person> jsonSerializer)
        {
            this.jsonSerializer = jsonSerializer;
        }
    }
```

* To represent JSON data in both its serialized and deserialized form, you can now do the following:

```csharp
    Person person = new Person { Name = "Lydia", Age = 24, Height = 160, Weight = 60 };
    string str = jsonSerializer.ToString(person);
    Person newPerson = jsonSerializer.FromString<Person>(str);
```
