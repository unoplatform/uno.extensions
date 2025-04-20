---  
uid: Reference.Storage  
---  

# Storage Reference  

This document introduces the storage-related functions provided by the `IStorage` interface. These functions allow you to interact with file storage in your application, including creating folders, reading files, and writing files.  

> [!NOTE]
> For Usage examples, refer to the [Storage](xref:Uno.Extensions.Storage.HandlingPackageFiles) documentation in Overview.

### 1. CreateFolderAsync  

**Description:**  
Creates a folder relative to the application data path.  

**Signature:**  
**Parameters:**  
- `foldername` (string): The name of the folder to create.  

**Returns:**  
- The folder path if successfully created, or `null` if the operation fails.  

---  

### ReadPackageFileAsync 

For this function, there are multiple overloads available. The following sections describe each overload in detail.

#### `Task<string?> ReadPackageFileAsync(string filename)`

**Description:**  
Reads the contents of a file from the application package.  

**Signature:**  
**Parameters:**  
- `filename` (string): The relative path to the file to read.  

**Returns:**  
- The text contents of the file if it can be read, or `null` if the file cannot be read.

---

#### `Task<TData?> ReadPackageFileAsync(ISerializer serializer,string filename)`

**Description:**  
Reads the contents of a file from the application package and deserializes it to the specified type.  

**Signature:**  
**Parameters:** 
- `serializer` (ISerializer): The serializer to use for deserialization.  
- `fileName` (string): The relative path of the file to read from.  

**Returns:**
- The deserialized instance of the specified type, or `null` if the file isn't found.  

---  

#### `Task<ImmutableList<string>?> ReadPackageFileAsync(string filename, List<(int Start,int End)> lineRanges)`

**Description:**  
Reads specific line ranges from a file in the application package.  

**Signature:**  
**Parameters:**  
- `filename` (string): The relative path to the file to read.  
- `lineRanges` (List<(int Start, int End)>): A list of tuples specifying the start and end line numbers to select and return.  

**Returns:**  
- An `ImmutableList<string>` containing the selected lines, or `null` if the file cannot be read.  

---  

### 5. OpenPackageFileAsync  

**Description:**  
Opens a file for reading from the application package.  

**Signature:**  
**Parameters:**  
- `filename` (string): The relative path to the file to read.  

**Returns:**  
- A `Stream` for the file if it can be opened, or `null` if the file cannot be opened.  

---  

### 6. WriteFileAsync  

**Description:**  
Writes text to a file relative to the application data path.  

**Signature:**  
**Parameters:**  
- `filename` (string): The relative path to the file to write.  
- `text` (string): The text to write to the file.  
- `overwrite` (bool): Whether to overwrite the file if it already exists.  

**Returns:**  
- An awaitable `Task`.  

---  
