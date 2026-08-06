---
uid: Uno.Extensions.Storage.HowToPackageFiles
---

# How To: Handling Package Files with Storage

The `IStorage` interface provides methods to work with files contained in your application's package.

These operations include:

- [How To: Handling Package Files with Storage](#how-to-handling-package-files-with-storage)
  - [Prerequisites](#prerequisites)
  - [Create Folder](#create-folder)
  - [Read File as string](#read-file-as-string)
  - [Read File with Serialization](#read-file-with-serialization)
  - [Open Package File as Stream](#open-package-file-as-stream)
  - [Writing to File](#writing-to-file)

## Prerequisites

[!INCLUDE [use-storage-prerequisites](./includes/use-storage-prerequisites.md)]

The following code samples to help you understand how to use these methods effectively in your application. All of them assuming to be used in a class or record definition of course.

## [Create Folder](#tab/create-folder)

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

## [Read File as string](#tab/read-file-as-string)

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
        Console.WriteLine($"File '{fileNameToRead}' not found in the package.");
    }
}
```

<!-- TODO: Uncomment this if PR #2734 has been merged https://github.com/unoplatform/uno.extensions/pull/2734

## [Reading specified Lines from Package File](#tab/reading-specified-lines-from-package-file)

You can also read just selected lines from a file, which is particularly useful for processing large unstructured text files, but you might only need certain lines of it, which can but not have to be in one Block of Lines.

```csharp
public async Task ReadSpecificLinesExample()
{
    var fileName = "example.txt";
    var lineRanges = new List<(int Start, int End)> // this could any type fitting as IEnumerable<(int,int)> since this function does not manipulate the provided Values.
    {
        (0, 5), // Read lines 0 to 5
        (10, 15) // Read lines 10 to 15
    };

    var selectedLines = await storage.ReadLinesFromPackageFileAsync(fileName, lineRanges);

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

> [!TIP]
> This function is expecting 1-based integer tuple pairs, so you could gracefully use common text editor's line numbering, to provide the appropriate values!

## [Read File with Serialization](#tab/read-file-with-Serialization)

You can get already deserialized data from your Package File content using the `StorageExtensions.ReadPackageFileAsync<TData>` method. This is ideal for scenarios like loading json formatted files without having to use `appsettings.json`.

> [!TIP]
> Make sure, your Model or ViewModel awaits this Typed Serializer.

```csharp
public SampleModel(ISerializer<Person> _serializer)
{
    public async Task ReadAndDeserializeFile(string fileName = "data.json")
    {

        var data = await _storage.ReadPackageFileAsync<Person>(_serializer, fileName);

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
> For example this could be a sample definition of `Person`, using [**`Uno.Extensions.Serialization`**](xref:Uno.Extensions.Serialization.Overview)
>
> ```csharp
> public record Person(string FirstName, string LastName);
>
> [JsonSerializable(typeof(Person))]
> public partial class PersonContext : JsonSerializerContext
> ```

## [Open Package File as Stream](#tab/open-package-file-as-stream)

If you need to open a file from your application's package as a stream, you can use the `OpenPackageFileAsync` method. This is useful for scenarios like reading binary data or processing files incrementally.

```csharp
public async Task OpenFileAsStreamExample()
{
    var fileName = "example.bin";
    // Ensure the stream is properly disposed of after use
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
    };
}
```

> [!CAUTION]
> **Important Note on Stream Handling**
>
> When working with streams, it is essential to ensure they are properly closed after use to prevent resource leaks and potential issues with file or network operations. In C#, this can be achieved by using a `using` statement, which ensures that the stream is disposed of correctly, even if an exception occurs during processing. The stream is closed automatically when it goes out of scope, simplifying resource management and improving code reliability.

## [Writing to File](#tab/write-to-file)

To write content to a file in your application's package, use the `WriteFileAsync` method. This method allows you to overwrite an existing file or create a new one.

```csharp
public async Task WriteFileExample(string fileName = "output.txt", string toBeWrittenContent = "Hello, Uno Platform!", bool shouldOverwrite = false)
{

    try
    {
        await storage.WriteFileAsync(fileName, toBeWrittenContent, shouldOverwrite);
        Console.WriteLine($"Successfully wrote to file '{fileName}'.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to write to file '{fileName}': {ex.Message}");
    }
}
```

---

[!INCLUDE [getting-help](../includes/getting-help.md)]
