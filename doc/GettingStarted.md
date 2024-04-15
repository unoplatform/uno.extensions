---
uid: Uno.Extensions.HowToGettingStarted
---
# How-To: Creating an application with Uno.Extensions

This tutorial will walk through how to create an Uno application with the `dotnet new` tool, which is already configured to use the Uno.Extensions.

## Step-by-steps

### 1. Installing extension templates

> [!NOTE]
> Make sure to setup your environment first by [following our instructions](xref:Uno.GetStarted.vs2022).

The `dotnet` templates included in the `Uno.Templates` package are used to easily create new projects that already reference the Uno.Extensions.

* Open a command prompt and run the following:

    ```dotnetcli
    dotnet new install Uno.Templates
    ```

* Navigate to the desired projects directory, and use the `unoapp` template to generate the starter solution discussed above

    ```dotnetcli
    dotnet new unoapp -o MyProjectName
    ```

    The argument specified after the `-o` flag (i.e. MyProjectName) will act as the name for both a containing directory and the generated solution.

* Open the solution in Visual Studio

    `.\MyProjectName\MyProjectName.sln`

### 2. Exploring the Solution

The generated solution will contain:

* *MyProjectName* - for application logic, and other constructs like view models and services, as well as the pages, controls and other views that make up the UI of the application.
* *MyProjectName/Platforms* - platform-specific folders for each supported platform.

    ![The structure of the generated solution](./Learn/images/ProjectStructure-min.png)

### 3. Running the Application

* Select a target from the drop-down as pictured below

    ![A screenshot of the generated targets](./Learn/images/GeneratedTargets-min.png)

* Click the "play" button, or press F5 to start debugging. The project will be compiled and deployed based on the target platform.