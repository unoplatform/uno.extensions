---
uid: Uno.Extensions.GettingStartedTests
---

### 1. Running the Unit Tests

* Right click the project inside Tests\\MyProjectName.Tests to open the context menu

* Select *Run Tests*

    The application will be compiled and the test cases will run.

> [!TIP]
> If the 'Run Tests' menu item doesn't exist, you need to Rebuild the solution to get Visual Studio to detect the available tests.

### 2. Running the UI tests

* Right click the MyProjectName.Wasm project to open the context menu

* Select *Set as startup project*

* Press Ctrl + F5 to start the WASM project without debugging.

* Once the application is compiled, it will launch inside your default browser. Take note of the URL which should look something like this: https://localhost:5000/Main

* Find the project *Tests\\MyProjectName.UI.Tests* and locate the *Constants.cs* file

* Open *Constants.cs* and update the WebAssemblyDefaultUri constant

    It should appear similar to this:

    ```cs
    public readonly static string WebAssemblyDefaultUri = "https://localhost:5000/";
    ```

* Go back to the project *Tests\\MyProjectName.UI.Tests* and right click. Then, *Run Tests*

    ![Test Explorer in VS](./Learn/images/TestExplorer-min.png)
