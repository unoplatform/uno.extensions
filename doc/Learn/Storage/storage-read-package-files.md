---
uid: Uno.Extensions.Storage.UsingStorageExtensions
---

# Handling Package Files with Storage

[!INCLUDE [use-storage-prerequirements](./includes/use-storage-prerequirements.md)]

## Usage Examples for Create, Read and Write Operations on Package Files

The `IStorage` interface provides methods to interact with files in your application's package. Below are some examples to help you understand how to use these methods effectively in your application. All of them have to be, of course, nested in a class or record.

### Creating a Folder in the Package

If you need to create a folder in your application's package, you can use the `CreateFolderAsync` method. This is helpful for organizing files into directories.

```csharp
public async Task CreateFolderExample()
{
    var folderName = "NewFolder";

    var folderPath = await _storage.CreateFolderAsync(folderName);

    if (folderPath is not null)
    {
        Console.WriteLine($"Successfully created folder at '{folderPath}'.");
    }
    else
    {
        Console.WriteLine($"Failed to create folder '{folderName}'.");
    }
}
```

### Reading whole File Content as string

You can read the contents of a file in your application's package as a string using the `ReadPackageFileAsync` method. This is useful for scenarios like loading configuration files or templates.

```csharp
public async Task ReadThisFile(string fileNameToRead = "example.txt")
{
    var fileContent = await _storage.ReadPackageFileAsync(fileNameToRead);

    if (fileContent is not null)
    {
        Console.WriteLine($"File Content:\n{fileContent}");
    }
    else
    {
        Console.WriteLine($"File '{fileName}' not found in the package.");
    }
}
```

<!-- TODO: Uncomment this if PR #2734 has been merged https://github.com/unoplatform/uno.extensions/pull/2734
### Reading Specific Lines from a File

If you need to read specific lines from a file, you can use the overload of `ReadPackageFileAsync` that accepts line ranges. This is particularly helpful for processing large files where only certain sections are needed.

```csharp
public async Task ReadSpecificLinesExample()
{
    var fileName = "example.txt";
    var lineRanges = new List<(int Start, int End)> // this could any type fitting as IEnumerable<(int,int)> since this function does not manipulate the provided Values.
    {
        (0, 5), // Read lines 0 to 5
        (10, 15) // Read lines 10 to 15
    };

    var selectedLines = await _storage.ReadLinesFromPackageFile(fileName, lineRanges);

    if (selectedLines is not null)
    {
        Console.WriteLine(line);
    }
    else
    {
        Console.WriteLine($"File '{fileName}' not found or empty.");
    }
}
``` -->

### Reading a File and getting its deserialized Content

You can deserialize the contents of a file into a specific type using the `StorageExtensions.ReadPackageFileAsync<TData>` method. This is ideal for scenarios like loading JSON configuration or data files.

> [!TIP]
> Make sure, your Model or ViewModel awaits this Typed Serializer.

```csharp
public SampleModel(ISerializer<Person> _serializer)
{
    public async Task ReadAndDeserializeFile(string fileName = "data.json")
    {

        var data = await storage.ReadPackageFileAsync<Person>(_serializer, fileName);

        if (data is not null)
        {
            Console.WriteLine($"Deserialized Data: {data}");
        }
        else
        {
            Console.WriteLine($"File '{fileName}' not found or could not be deserialized.");
        }
    }
}
```

> [!TIP]
> For example, this could be easily be done using [**`Uno.Extensions.Serialization`**](../Serialization/SerializationOverview.md)
>
> ```csharp
> public record Person(string FirstName, string LastName);
>
> [JsonSerializable(typeof(Person))]
> public partial class PersonContext : JsonSerializerContext
> ```

### Opening a File as a Stream

If you need to open a file from your application's package as a stream, you can use the `OpenPackageFileAsync` method. This is useful for scenarios like reading binary data or processing files incrementally.

```csharp
public async Task OpenFileAsStreamExample()
{
    var fileName = "example.bin";

    using var stream = await storage.OpenPackageFileAsync(fileName)
    {
        if (stream is not null)
        {
            Console.WriteLine($"Successfully opened file '{fileName}' as a stream.");
            // Process the stream as needed
        }
        else
        {
            Console.WriteLine($"File '{fileName}' not found in the package.");
        }
    }
}
```

> [!TIP]
> When working with streams, it is essential to ensure they are properly closed after use to prevent resource leaks and potential issues with file or network operations. In C#, this can be achieved by using a `using` statement, which ensures that the stream is disposed of correctly, even if an exception occurs during processing.
>
> By using the `using` statement, you can guarantee that the stream is closed automatically when it goes out of scope, simplifying resource management and improving code reliability.

### Writing to a File in the Package

To write content to a file in your application's package, use the `WriteFileAsync` method. This method allows you to overwrite an existing file or create a new one.

```csharp
public async Task WriteFileExample(string fileName = "output.txt", string toBeWrittenContent = "Hello, Uno Platform!", bool shouldOverwrite = false)
{

    try
    {
        await storage.WriteFileAsync(fileName, content, overwrite);
        Console.WriteLine($"Successfully wrote to file '{fileName}'.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to write to file '{fileName}': {ex.Message}");
    }
}
```

---

These examples demonstrate how to use the `IStorage` interface and its extensions to read files from your application's package, referring to the [Reference](xref:Reference.Storage).
