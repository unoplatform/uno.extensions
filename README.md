# Uno Application Platform Template

This is a multi-platform app project template using [Uno](https://github.com/unoplatform/uno) and the latest .NET practices.

## Getting Started

We use `dotnet` project templates to easily create new projects. It simplifies the **project renaming** and supports **conditional inclusions**.

### Installing and uninstalling the template

1. In order to install the template, clone this repository on your machine and open a command prompt at the root of the project and run the following command 

   `dotnet new -i ./`


1. If you want to uninstall the template, run the following command.

    `dotnet new -u`

    This will list you the list of installed templates, look for this template and copy the command with the absolute path like this. (Note the quotes added, otherwise it doesn't work)

    `dotnet new -u "C:\P\ApplicationPlatformTemplate"`

### Running the template to generate a new project

1. To run the template and create a new project, run the following command.

    `dotnet new uno-platform -n MyProjectName`

    The following options are available when running the command.

    - To get help: `dotnet new uno-platform -h`
    - ... TBD ...  additional options may be added as the template progresses

[Read this for more information on custom templates](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates).

## Documentation

This repository provides documentation on different topics under the [doc](doc/) folder.

- [Architecture](doc/Architecture.md)
- [BuildPipeline](doc/BuildPipeline.md)
- [Configuration](doc/Configuration.md)
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

## Changelog

Please consult the [CHANGELOG](CHANGELOG.md) for more information about the version history.

## License

This project is licensed under the Apache 2.0 license. See the [LICENSE](LICENSE) for details.

## Contributing

Please read [CONTRIBUTING](CONTRIBUTING.md) for details on the process for contributing to this project.

Be mindful of our [Code of Conduct](CODE_OF_CONDUCT.md).
