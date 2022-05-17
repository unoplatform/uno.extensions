# Uno Extensions + Application Template

## Overview

The unoapp-extensions template uses Uno.Extensions.Hosting to configure the hosting environment for the application. This includes loading configuration information, setting up a dependency container and configure logging for the application. The template is already pre-configured so all you need to do is add your project dependencies, such as services for loading data, and register the routes for your application.

The template is a typical Uno application that makes used of a Shared project for all the application logic and corresponding platform specific projects for each supported platform. The purpose of this template is to provide a starting point for real world applications, making it easy to generate production ready applications.

In addition to referencing Uno, the Guidance Template also references the following packages.

| Package                      |                                                                                                           |
|------------------------------|-----------------------------------------------------------------------------------------------------------|
| CommunityToolkit.Mvvm        | Provides base implementations for INotifyPropertyChanged, INotifyDataErrorInfo and other helper utilities |
| Uno.Extensions.Configuration | Loads configuration information from various sources                                                      |
| Uno.Extensions.Hosting       | Initializes the hosting environment, including initialising the dependency container                      |
| Uno.Extensions.Http          | Configures HttpClient to use native http handlers                                                         |
| Uno.Extensions.Localization  | Provides access to localizable resources                                                                  |
| Uno.Extensions.Logging       | Configures application logging                                                                            |
| Uno.Extensions.Navigation    | Routing framework for frame based navigation                                                              |
| Uno.Extensions.Reactive      | Development framework for reactive applications                                                          |
| Uno.Extensions.Serialization | Helpers for json serialization                                                                            |

## Uno.Extensions

The Uno.Extensions is a series of packages designed to encapsulate common developer tasks associated with building cross platform mobile, desktop and web applications using the Uno platform.
