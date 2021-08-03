# Uno Extensions + Application Template

## Overview

**TODO: Need to confirm what the name of the template is. It's referred to here as the Guidance Template but the final name will need to be defined.**

The Uno.Platform Guidance template is a typical Uno application that makes used of a Shared project for all the application logic and corresponding platform specific projects for each supported platform. The purpose of this template is to provide a starting point for real world applications, making it easy to generate production ready applications. 

The Guidance Template is based on WinUI 3 / Project Reunion. It also includes a UWP project which uses WinUI 2.x for compatibility. 

In addition to referencing Uno, the Guidance Template also references the following packages.

| Package                      |                                                                                                           |
|------------------------------|-----------------------------------------------------------------------------------------------------------|
| CommunityToolkit.Mvvm        | Provides base implementations for INotifyPropertyChanged, INotifyDataErrorInfo and other helper utilities |
| Uno.Extensions.Configuration | Loads configuration information from various sources                                                                                                          |
| Uno.Extensions.Hosting       | Initializes the hosting environment, including initialising the dependency container                                                                                                           |
| Uno.Extensions.Http          | Configures HttpClient to use native http handlers                                                                                                          |
| Uno.Extensions.Localization  | Provides access to localizable resources                                                                                                          |
| Uno.Extensions.Logging       | Configures application logging                                                                                                          |
| Uno.Extensions.Navigation    | Routing framework for frame based navigation                                                                                                          |
| Uno.Extensions.Serialization | Helpers for json serialization                                                                                                          |

## Uno.Extensions

The Uno.Extensions is a series of packages designed to encapsulate common developer tasks associated with building cross platform mobile, desktop and web applications using the Uno platform. 

Each package is delivered as source code, making it possible for developers to extend or adapt the code without having to be concerned with bloating their application with unnecessary assemblies, or running into issues with compatibility issues between different package versions. 

The Uno.Extensions support both WinUI and UWP. However, in order to handle the change in namespaces it's necessary to include the UNO_UWP_COMPATIBILITY compilation constant in the csproj eg:
<DefineConstants>$(DefineConstants);UNO_UWP_COMPATIBILITY</DefineConstants>


