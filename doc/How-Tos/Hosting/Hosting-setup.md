# How to get started with hosting

Hosting can be used to register services that will be accessible throughout the application via dependency injection. This tutorial will walk you through the critical pieces needed to leverage hosting in your application. 

NOTE: If you used the `dotnet new` template to create the solution, some of the steps outlined here are unecessary. You can see whether your application already creates the IHost interface using the CreateDefaultBuilder method by looking for a generated `App.xaml.host.cs` file.

## Step-by-steps
1. Install the Uno.Extensions.Hosting library from NuGet, making sure you reference the package that is appropriate for the flavor of Uno in use.

2. Set the LangVersion to leverage new C# features
    * Create a new file named Directory.Build.props in the solution folder
    * Add the following lines to the file:
        ```xml 
        <Project>
            <PropertyGroup>
                <LangVersion>10.0</LangVersion>
            </PropertyGroup>
        </Project>
        ```
3. Use the UnoHost static class to create IHost for the application when the application instance is created:
    * We need to expose the IHost instance to the rest of the application. Add the following property to your App.xaml.cs class:
        ```cs
        public IHost Host { get; init; }
        ```
    * Invoke the CreateDefaultBuilder() method in UnoHost followed by Build() 
        ```cs
        public App()
        {
            Host = UnoHost
                .CreateDefaultBuilder()
                .Build();
        ...
        ```