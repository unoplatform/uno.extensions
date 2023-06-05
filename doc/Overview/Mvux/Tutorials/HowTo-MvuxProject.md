---
uid: Overview.Mvux.HowToMvuxProject
---

# How to set up an MVUX project

In this tutorial you'll learn how to set up an Uno Platform project to use MVUX. 

1. Make sure your environment is set up properly by using [uno check](xref:UnoCheck.UsingUnoCheck).
1. You can create a Uno App by either using the Uno Platform Visual Studio extension or via the command line interface.

    # [**Visual Studio**](#tab/vs)

    #### Using Visual Studio 2022 Uno Platform Extension

    - Make sure you have the latest version of Uno Extension (v4.8 or higher) installed.

        ![Screenshot displaying how to check the version of the Uno Extension wizard version in Visual Studio extension manager](../Assets/MvuxProject-VsixVersion.jpg)

    - Press <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> to create a new project and select "Uno Platform App".

    - Give your project an appropriate name.

    - When prompted Select *Blank* and click *Customize*

        ![Screenshot displaying the intro screen of the Uno Extension wizard in Visual Studio](../Assets/MvuxProject-StartupType.jpg)

    - In the *Presentation* tab (3rd one), select MVUX.

        ![Screenshot displaying how to pre-install MVUX in the generated project](../Assets/MvuxProject-Mvux.jpg)

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
