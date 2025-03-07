---
uid: Reference.Storage.Methods
---

# Storage Methods

This section provides detailed documentation for each method available in the `IStorage` interface. Each method is described with its purpose, parameters, return values, and usage examples.

## CreateFolderAsync

```csharp
Task<string?> CreateFolderAsync(string foldername);
```

**Description:**
Creates a folder relative to the application's data directory.

**Parameters:**

- `foldername` *(string)*: The name of the folder to create.

**Returns:**

- `Task<string?>`: The folder path if successfully created; otherwise, `null`.

**Example Usage:**

```csharp
string? folderPath = await storage.CreateFolderAsync("MyAppData");
if (folderPath != null)
{
    Console.WriteLine($"Folder created at: {folderPath}");
}
```

---

## ReadPackageFileAsync

```csharp
Task<string?> ReadPackageFileAsync(string filename);
```

**Description:**
Reads a file from the application package and returns its contents as a string.

**Parameters:**

- `filename` *(string)*: The relative path to the file to read.

**Returns:**

- `Task<string?>`: The text contents of the file if it can be read; otherwise, `null`.

**Example Usage:**

```csharp
string? content = await storage.ReadPackageFileAsync("config.json");
if (content != null)
{
    Console.WriteLine($"File content: {content}");
}
```

---

## OpenPackageFileAsync

```csharp
Task<Stream?> OpenPackageFileAsync(string filename);
```

**Description:**
Opens a file for reading from the application package and returns a stream.

**Parameters:**

- `filename` *(string)*: The relative path to the file to read.

**Returns:**

- `Task<Stream?>`: A stream for the file if it can be opened; otherwise, `null`.

**Example Usage:**

```csharp
using Stream? fileStream = await storage.OpenPackageFileAsync("data.txt");
if (fileStream != null)
{
    using StreamReader reader = new StreamReader(fileStream);
    string content = await reader.ReadToEndAsync();
    Console.WriteLine($"File content: {content}");
}
```

---

## WriteFileAsync

```csharp
Task WriteFileAsync(string filename, string text, bool overwrite);
```

**Description:**
Writes the specified text to a file in the application's data directory.

**Parameters:**

- `filename` *(string)*: The relative path to the file to write.
- `text` *(string)*: The text content to write.
- `overwrite` *(bool)*: Determines whether an existing file should be overwritten.

**Returns:**

- `Task`: An awaitable task that completes when the operation finishes.

**Example Usage:**

```csharp
await storage.WriteFileAsync("log.txt", "Application started", true);
Console.WriteLine("Log file updated.");
```

---

[!include[getting-help](includes/getting-help.md)]
