---
uid: Overview.Reactive.MvuxProject
---

# How to set up an MVUX project

In this tutorial you'll learn how to set up an Uno Platform project to use MVUX. 

1. Make sure your environment is set up properly by using [uno check](/docs/articles/external/uno.check/doc/using-uno-check.html).
1. You can create a Uno App by either using the Uno Platform Visual Studio extension or via the command line interface.

    # [**Visual Studio**](#tab/vs)

    #### Using Visual Studio 2022 Uno Platform Extension

    - Make sure you have the latest version of Uno Extension (v4.8) installed.

        ![](Assets/MvuxProject-VsixVersion.jpg)

    - Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> to create a new project and select "Uno Platform App".

    - Give your project an appropriate name.

    - When prompted Select *Blank* and click *Customize*

        ![](Assets/MvuxProject-StartupType.jpg)

    - In the *Presentation* tab (3rd one), select MVUX.

        ![](Assets/MvuxProject-Mvux.jpg)

    - Click *Create* on the bottom right corner.    
    
    # [**CLI**](#tab/cli)
    
    #### Using the command line interface
    
    - Run the following command, using an appropriate name (`MyApp` in this page).
    
        ```cmd
        dotnet new unoapp -preset blank -presentation mvux -o MyApp
        ```
      
        Refer to [this](https://platform.uno/docs/articles/get-started-dotnet-new.html) article for more details
        on using the CLI interface of creating projects.

    - Launch the created solution, MyApp.sln, in  Visual Studio or Visual Studio Code.
