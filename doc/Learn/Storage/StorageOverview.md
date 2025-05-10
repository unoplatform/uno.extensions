---
uid: Uno.Extensions.Storage.Overview
---
<!-- markdownlint-disable MD033 MD036-->
# Storage Overview

Uno.Extensions.Storage facilitate local data storage across multiple platforms, including WebAssembly, Android, iOS, macOS, Desktop and Windows. This extension is particularly useful for applications that require secure, persistent storage of user preferences, configuration settings, and sensitive information such as tokens and credentials.

**Wrapped up:** Storage feature allows you to read and write files in your app's local storage, create additional folders, like you may of the `System.IO` functionality, but with the nice benefit of being platform independent, injectable through Dependency Injection (DI) and provides you the Opportunity, to provide an `ISerializer` from [Uno.Extensions.Serialization](xref:Uno.Extensions.Serialization.Overview) to allow you to individualize the Serialization or/and get back directly the corresponding Type your App needs!

## Explore the Storage Feature and Tutorials

- **Start here:** [Getting Started with Storage](xref:Uno.Extensions.Storage.GettingStarted)
- Handling Package Files
  - [Creating Folders]
  - [Reading Package Files]
  - [Opening Package Files to read the Content directly as Stream]
  - [Writing to Package Files]

Additionally there are some more useful Extensions you get with this Package:

- [Get Serialized Data from your Package Files]
<!-- TODO: Uncomment this after https://github.com/unoplatform/uno.extensions/pull/2734 has been merged  - [Read specific lines of a package file]
  - [Get specific Items from a `IEnumerable<string>`] -->

- [How to: Required Entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements)

> [!IMPORTANT]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, uses the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.
> [!IMPORTANT]
> To recognize Problems your App might have while this, it's recommended to use [Logging](xref:Uno.Extensions.Logging.Overview) or the Non-Extension [Uno.UI.Logging](https://platform.uno/docs/articles/logging.html)

## Which files could be preferred to use Storage instead of `appsettings.json` via `IConfiguration`

As simple as it sounds, you can use the Storage feature to read and write files (*almost*) independent of their type, while `appsettings.json` is a JSON file that is used to store application settings and configuration data.

> [!TIP]
> Here is a rule for Storage Package files to remember:
>
> As long as you can provide a [ISerializer](https://github.com/unoplatform/uno.extensions/blob/main/src/Uno.Extensions.Serialization/ISerializer.cs) or your file is generally readable as <see langword="string"/> or <see cref="Stream"/>, this can be handled with Uno.Extensions.Storage.
> [!NOTE]
> Keep in mind, that for other files you might have their BuildAction property set to Resources or Content, you only need to call them via `ms-appx:Here/comes/your/filepath.txt` for example to get the file from the package.

### Classic file types to use with Storage

- .txt -> simply get the content as a <see langword="string"/> <!--TODO: Uncomment this if PR #2734 has been merged https://github.com/unoplatform/uno.extensions/pull/2734 // or use the [`IEnumerable<string> StorageExtensions.ReadLinesFromPackageFile(string filename, IEnumerable<(int Start, int End)> lineRanges)`](xref:Uno.Extensions.Storage.StorageExtensions.ReadLinesFromPackageFile) Extension in this Feature Package to define, which line ranges you want to get from this a file.-->
- .xml -> could be Serialized/Deserialized with an appropriate `XmlSerializer`
- .json -> could be Serialized/Deserialized by using the `Uno.Extensions.Serialization` Extension
- .csv -> could be Serialized/Deserialized with an appropriate `CsvSerializer`

### Useful additional Serializer Libraries for more special files

> [!NOTE]
> This List is not guaranteed to be complete!

- .docx/.xlsx
  - `OpenXML`
  - SyncFusion (all payed)
    - [Word Framework](https://www.syncfusion.com/document-processing/word-framework/winui)
    - [Excel Framework](https://www.syncfusion.com/document-processing/excel-framework/winui)
    - [PowerPoint Framework](https://www.syncfusion.com/document-processing/presentation-framework/winui)
- .pdf
  - [PdfPig (Free & Open Source)](https://github.com/UglyToad/PdfPig)
  - [PDFsharp (Free & Open Source, payed advanced support)](https://pdfsharp.com/)
  - [SyncFusion (Payed)](https://www.syncfusion.com/document-processing/pdf-framework/winui) this has many capabilities, which might make you consider using it either way, if you use the MAUI Embedding feature in .NET 8 Uno Apps.

### File types you can not use Storage for

**Binary files like:**

- .exe
- .dll

**Compressed files like:**

- .zip
- .tar
- .gz

*They have to be extracted before usage*

Multimedia files like:

- .mp3
- .mp4
- .jpg
- .png
- .gif

> [!TIP]
> You can handle Image files like .png and .svg via [***Uno.Resizetizer***](xref:Uno.Resizetizer.GettingStarted), which is present in every Uno Project by default.

## Next steps

Check out the [Getting Started Guide](xref:Uno.Extensions.Storage.GettingStarted)

## See Also

- [Uno.UI.Toolkit.StorageFileHelper](https://platform.uno/docs/articles/features/file-management.html)
- Uno specifics for [File and Folder Pickers](https://platform.uno/docs/articles/features/windows-storage-pickers.html)
- Storing and reading [Settings](https://platform.uno/docs/articles/features/settings.html)

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
