---
uid: Reference.Storage.Extensions
---

# Storage Extensions

This section provides documentation for extension methods that enhance the functionality of the `IStorage` interface.

## ReadPackageFileAsync

```csharp
public static async Task<TData?> ReadPackageFileAsync<TData>(this IStorage storage, ISerializer serializer, string fileName);
```

**Description:**
Reads the contents of a file and deserializes it into the specified type using a provided serializer.

**Type Parameters:**

- `TData`: The type to which the file content should be deserialized.

**Parameters:**

- `storage` *(IStorage)*: The storage instance used to access the file.
- `serializer` *(ISerializer)*: The serializer responsible for converting the file content.
- `fileName` *(string)*: The relative path of the file to read.

**Returns:**

- `Task<TData?>`: The deserialized instance read from the file, or `null` if the file is not found.

**Example Usage:**

```csharp
var data = await storage.ReadPackageFileAsync<MyDataClass>(serializer, "config.json");
if (data != null)
{
    Console.WriteLine("Successfully read and deserialized file.");
}
```
