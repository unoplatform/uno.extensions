---
uid: Overview.Reactive.HowTos.CreateMvuxProject
---

# How to set up an MVUX project

In this tutorial you'll learn how to set up an Uno Platform project ready to use with the MVUX architecture and its tools.

### Creating an Uno Platform project

1. Make sure your environment is set up properly by using [uno check](external/uno.check/doc/using-uno-check.md).
1. Create a new Uno App by following steps 1-5 in [this](https://platform.uno/docs/articles/getting-started-tutorial-1.html) tutorial
when using the [Visual Studio Uno templates](https://platform.uno/docs/articles/get-started-vs-2022.html#install-the-solution-templates),
or by running the following command, using an appropriate name (`MyAppName` in this page).

  ```cmd
  dotnet new unoapp-uwp -o MyAppName
  ```

  Refer to [this](https://platform.uno/docs/articles/get-started-dotnet-new.html) article for more details
  on using the CLI interface of creating projects.

> [!NOTE] 
> Make sure .NET 7 or above is selected, as we'll be using some of the recent C# features in these tutorials,
such as [records](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record).

<!--
In the newly created solution you'll find multiple projects each targeting a different platform and another central one
which is shared/referenced from all projects, this project (PeopleApp without any suffixes) is where most of the work is done. Let's call it the base project, and this is where all the files are to be added onwards.
 -->

1. Right-click on `MyAppName` project (or the name you gave it) and select `Manage NuGet Packages for Solution` from the context menu.
    - Make sure to select **nuget.org** or **NuGet official package source** as the package source
    - Click on the Updates tab. Update the following packages to the latest stable version,
    if they're not up to date: `Uno.WinUI`, `Uno.UI.WebAssembly` `Uno.Wasm.Bootstrap`, and `Uno.Wasm.Bootstrap.DevServer`.

1. Click back on the **Browse** tab and install the following NuGet Packages to the `MyAppName` project: `Uno.Extensions.Reactive.WinUI`.

![](Assets/NuGetPackage.jpg)
