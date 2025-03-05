---
uid: Uno.Extensions.Storage.PackageFiles.Intro
---

# Introduction: Handling Package Files in your App

Apps may be packaged and stored in specific folders like `AppData`, which can either be user-specific or installed globally. To help you efficiently manage the files your app needs at runtime or beyond, the Uno Storage Extension provides a platform-specific implementation to simplify this process.

Here is a overview of the Tasks it can help you with, als core part the IStorage Interface:

* [Creating Folders]
* [Reading Package Files]
* [Opening Package Files to read the Content directly as Stream]
* [Writing to Package Files]

Additionally there are some more useful Extensions you get with this Package:

* [Get Serialized Data from your Package Files]
<!-- TODO: Uncomment this after https://github.com/unoplatform/uno.extensions/pull/2734 has been merged * [Read specific lines of a package file]
* [Get specific Items from a `IEnumerable<string>`] -->

> [!NOTE]
> All of the Tasks above are implying, the files/folders, you want to interact with, are nested to the AppData Path your App is executing from.
> [!IMPORTANT]
> To recognize Problems your App might have while this, it's recommended to use [Logging](xref:Uno.Extensions.Logging.Overview) or the Non-Extension [Uno.UI.Logging](https://platform.uno/docs/articles/logging.html)

---

**Previous Readings:** [Storage Overview](xref:Uno.Extensions.Storage.Overview) | **Next step:** Check out the [Getting Started Guide](xref:Uno.Extensions.Storage.GettingStarted) | **Samples** [How-To: Handling Package Files](xref:Uno.Extensions.Storage.PackageFiles.HowToPackageFiles)

---

[!INCLUDE [getting-help](./includes/getting-help.md)]
