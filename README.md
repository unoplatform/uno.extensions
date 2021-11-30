# Uno Application Platform Template

This is a multi-platform app project template using [Uno](https://github.com/unoplatform/uno) and the latest .NET practices.

## Getting Started

We use `dotnet` project templates to easily create new projects. It simplifies the **project renaming** and supports **conditional inclusions**.

### Installing and uninstalling the template

1. In order to install the template, clone this repository on your machine and open a command prompt at the root of the project and run the following command

   `dotnet new -i Uno.Extensions.Templates`


1. If you want to uninstall the template, run the following command.

    `dotnet new -u Uno.Extensions.Templates`

### Running the template to generate a new project

1. To run the template and create a new project, run the following command.

    `dotnet new unoapp-extensions --material -o MyProjectName`

## Documentation

This repository provides documentation on different topics under the [doc](doc/) folder.

- [Architecture](doc/Architecture.md)
- [BuildPipeline](doc/BuildPipeline.md)
- [Configuration](doc/Configuration.md)
- [Code Style](doc/CodeStyle.md)
- [Data Loading](doc/DataLoading.md)
- [Dependency Injection](doc/DependencyInjection.md)
- [Environments](doc/Environments.md)
- [Error handling](doc/ErrorHandling.md)
- [HTTP](doc/HTTP.md)
- [Localization](doc/Localization.md)
- [Logging](doc/Logging.md)
- [Mvvm](doc/Mvvm.md)
- [Navigation](doc/Navigation.md)
- [Platform specifics](doc/PlatformSpecifics.md)
- [Scheduling](doc/Scheduling.md)
- [Serialization](doc/Serialization.md)
- [Startup](doc/Startup.md)
- [Testing](doc/Testing.md)
- [Validation](doc/Validation.md)
- [Reactive](doc/Reactive.md)

## License

This project is licensed under the Apache 2.0 license. See the [LICENSE](LICENSE) for details.

## Contributing

Be mindful of our [Code of Conduct](CODE_OF_CONDUCT.md).
