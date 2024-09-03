---
uid: Uno.Extensions.Markup.HowToMarkupProjectToolkit
---

# How to set up an C# Markup project for Toolkit

In this tutorial, you'll learn how to set up an Uno Platform project to use C# Markup.

1. Make sure your environment is set up properly by using [uno check](xref:UnoCheck.UsingUnoCheck).
1. You can create a Uno App by either using the Uno Platform Visual Studio extension or via the command line interface.

    ## [**Visual Studio**](#tab/vs)

    ### Using Visual Studio 2022 Uno Platform Extension

    - Open the Visual Studio and select Extensions => Manage Extensions and Search (<kbd>Ctrl</kbd> + <kbd>E</kbd>) for unoplatform.

    - Make sure you have the latest version of Uno Extension (v5.0 or higher) installed.

        ![Screenshot displaying how to check the version of the Uno Extension wizard version in Visual Studio extension manager](../Assets/MarkupProject-VsixVersion.jpg)

    - Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> to create a new project and select "Uno Platform App".

    - Give your project an appropriate name (`MySampleToolkitProject` in this page).

    - When prompted Select *Blank* and click *Customize*

        ![Screenshot displaying the intro screen of the Uno Extension wizard in Visual Studio](../Assets/MarkupProject-StartupType.jpg)

    - In the *Markup* tab (4rd one), select C# Markup.

        ![Screenshot displaying how to pre-install C# Markup in the generated project](../Assets/MarkupProject-VsixMarkup.jpg)

    - In the *Features* tab (7rd one), select Toolkit.

        ![Screenshot displaying how to pre-install Toolkit in the generated project](../Assets/MarkupProject-VsixMarkupToolkit.jpg)

    - Click *Create* on the bottom right corner.

    ## [**CLI**](#tab/cli)

    ### Using the command line interface

    - Run the following command, using an appropriate name (`MySampleToolkitProject` in this page).

        ```cmd
        dotnet new unoapp -preset blank -toolkit true -markup csharp -o MySampleToolkitProject
        ```

        Refer to [this](https://platform.uno/docs/articles/get-started-dotnet-new.html) article for more details
        on using the CLI interface of creating projects.

    - Launch the created solution, MySampleToolkitProject.sln, in  Visual Studio or Visual Studio Code.

## Toolkit References

Looking in detail at the projects with the addition of the Toolkit, we can see the addition of some additional references.
This allows an easy inclusion of the Toolkit in existing projects.

1. Add the references to the Directory.Packages.props file (with the updated version).

    ```xml
    <PackageVersion Include="Uno.Toolkit.WinUI" Version="0.0.0" />
    <PackageVersion Include="Uno.Toolkit.WinUI.Markup" Version="0.0.0" />
    ```

1. Add the package to the following projects:

    - PROJECT_NAME.Wasm.csproj
    - PROJECT_NAME.Mobile.csproj (or PROJECT_NAME.iOS.csproj, PROJECT_NAME.Droid.csproj, PROJECT_NAME.macOS.csproj if you have an existing project)
    - PROJECT_NAME.Skia.Gtk.csproj
    - PROJECT_NAME.Windows.csproj (or PROJECT_NAME.UWP.csproj for existing projects)

    ```xml
    <PackageReference Include="Uno.Toolkit.WinUI.Markup" />
    ```

Choose either:

- The *Uno.Toolkit.UI* package when targeting Xamarin/UWP
- The *Uno.Toolkit.WinUI* package when targeting net6.0+/WinUI
