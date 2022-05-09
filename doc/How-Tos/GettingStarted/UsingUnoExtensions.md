# How to use Uno.Extensions

Uno.Extensions is a series of NuGet packages designed to encapsulate common developer tasks associated with building multi-platform mobile, desktop and web applications using the Uno platform.

The Uno.Extensions follows the Microsoft.Extensions model that creates a host environment where you can register additional dependencies. The registered dependencies are then available throughout the application via Services (IServiceProvider) property on the IHost instance.

For a more specific description of the functionality included in each referenced package—such as
Configuration, Logging, Navigation, and
Reactive—refer to the relevant Uno.Extensions documentation. 

This tutorial will walk through how to create an Uno application with the `dotnet new` tool, that makes use of Uno.Extensions. 

## Getting Started

`dotnet` project templates are used to easily create new projects. It simplifies the project renaming and supports conditional inclusions.

**Installing and generating a set of projects from the template**

1. Open a command prompt and run the following

    `dotnet new -i Uno.Extensions.Templates`

2. Navigate to the desired projects directory, and use the unoapp-extensions template to generate the starter solution discussed above

    `dotnet new unoapp-extensions -o MyProjectName`

    The argument specified after the -o flag will act as the name for both a containing directory and the generated solution, so it is not required to create a new directory for the output.

3. Open the solution in Visual Studio

    `.\MyProjectName\MyProjectName.sln`

## Exploring the generated targets

The generated solution will contain:

- The *MyAppName*.Shared project for controls, pages, and views comprising the app’s UI layer
- A class library for application logic, and other constructs like view models and services 
- Platform-specific projects for each supported platform, each including additional package references
- Two more projects which contain either unit or UI tests that you will author for various application scenarios

![A screenshot of the generated targets](/doc/images/GeneratedTargets-min.png)

**Running a specific platform target in Visual Studio**

1. Select a target from the dropdown as pictured above

2. Press “run”. The necessary projects in the solution will be compiled and deployed based on the target platform

## Software testing

Because Uno.Extensions encapsulates a growing set of tasks associated with building outstanding multi-platform applications, it makes an opinionated assumption about developer coding practices. It should be the stance of the developer to incorporate automated tests to uncover defects, regressions, or any other erroneous discrepancy between expected and actual output. Using both unit and UI testing to reduce this uncertainty is a principle of sound software development.

![The structure of the generated solution](/doc/images/ProjectStructure-min.png)

Hence, Uno.Extensions includes projects for both as part of the standard new application template.

**Running the unit tests**

1. Right click the project inside Tests\\MyProjectName.Tests to open the context menu

2. Select *Run Tests*

The application will be compiled and the test cases will run.

**Running the UI tests**

1. Right click the MyProjectName.Wasm project to open the context menu

2. Select *Set as startup project*

3. Press Ctrl + F5 to start the WASM project without debugging.

4. Once the application is compiled, it will launch inside your default browser. Take note of the URL which should look something like this: https://localhost:11111/

5. Find the project Tests\\MyProjectName.UI.Tests and observe that a Constants.cs file exists inside

6. Open the Constants.cs file and update the WebAssemblyDefaultUri constant

    It should appear similar to this: 

    `public readonly static string WebAssemblyDefaultUri = "https://localhost:11111/";`

7. Go back to the project Tests\\MyProjectName.UI.Tests and right click. Then, *Run Tests*

**Viewing test results**

The results of both test types can now be inspected from the Test Explorer of Visual Studio (Ctrl + E, T). The *Tests* output in the Output window may also provide helpful information. When making changes to the application, remember to close the browser windows in order for a new version of the application to be properly loaded the next time UI tests are run.