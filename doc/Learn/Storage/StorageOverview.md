---
uid: Uno.Extensions.Storage.Overview
---
<!-- markdownlint-disable MD033 MD036-->
# Storage Overview

Uno.Extensions.Storage facilitate local data storage across multiple platforms, including WebAssembly, Android, iOS, macOS, Desktop and Windows. This extension is particularly useful for applications that require secure, persistent storage of user preferences, configuration settings, and sensitive information such as tokens and credentials.

But in general Storage feature allows you to read and write files in your app's local storage but also create additional folders, as you may know as regular `System.IO` functionality, just platform independent and working just fine with Dependency Injection (DI) and the Uno.Extensions.Serialization Extension.

> [!IMPORTANT]
> On Apple platforms (iOS, Mac Catalyst) the Uno storage extension, used by the authentication extension, uses the OS Key Chain service to store secrets. This requires your application to have the [proper entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements) to work properly.

## Explore the Storage Feature

- [Getting Started with Storage](xref:Uno.Extensions.Storage.GettingStarted)

- [How to: Required Entitlements](xref:Uno.Extensions.Storage.HowToRequiredEntitlements)

## Visit the Reference

- [Uno.Extensions.Storage Reference](xref:Uno.Extensions.Storage)

## Which files could be preferred to use Storage instead of `appsettings.json` via `IConfiguration`

As simple as it sounds, you can use the Storage feature to read and write files independent of their type, while `appsettings.json` is a JSON file that is used to store application settings and configuration data.

To name only some of the possible types:

- .txt -> simply get the content as a <see langword="string"/> or use the [`IEnumerable<string> StorageExtensions.ReadLinesFromPackageFile(string filename, IEnumerable<(int Start, int End)> lineRanges)`](xref:Uno.Extensions.Storage.StorageExtensions.ReadLinesFromPackageFile) Extension in this Feature Package to define, which line ranges you want to get from this a file.
- .xml -> could be Serialized/Deserialized with an appropriate `XmlSerializer`
- .json -> could be Serialized/Deserialized by using the `Uno.Extensions.Serialization` Extension
- .csv -> could be Serialized/Deserialized with an appropriate `CsvSerializer`

Additional Libraries required for serialization/deserialization:

- .docx/.xlsx
  - `OpenXML`
  - SyncFusion (all payed)
    - [Word Framework](https://www.syncfusion.com/document-processing/word-framework/winui)
    - [Excel Framework](https://www.syncfusion.com/document-processing/excel-framework/winui)
    - [PowerPoint Framework](https://www.syncfusion.com/document-processing/presentation-framework/winui)
- .pdf
  - [PdfPig (Free & Open Source)](https://github.com/UglyToad/PdfPig)
  - [PDFsharp (Free & Open Source, payed advanced support)](https://pdfsharp.com/)
  - [Syncfusion (Payed)](https://www.syncfusion.com/document-processing/pdf-framework/winui) this has many capabilities, which might make you consider using it either way, if you use the MAUI Embedding feature in .NET 8 Uno Apps.

The only limitation is, that they have to be readable as <see langword="string"/> or <see cref="Stream"/>.

## Excluded file types

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

But you can handle Image files via [***Uno.Resizetizer***](xref:Uno.Resizetizer.GettingStarted), which is present in every Uno Project by default, so you can use it to get this kind of files also available in your app.

## See Also

- [Uno.UI.Toolkit.StorageFileHelper](https://platform.uno/docs/articles/features/file-management.html)
- Uno specifics for [File and Folder Pickers](https://platform.uno/docs/articles/features/windows-storage-pickers.html)
- Storing and reading [Settings](https://platform.uno/docs/articles/features/settings.html)

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
