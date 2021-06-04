# Solution Architecture

## Context

The Uno.Platform application template is a typical Uno application that makes used of a Shared project for all the application logic and corresponding platform specific projects for each supported platform. The template is based on WinUI 3 / Project Reunion but also has a UWP project which uses WinUI 2.x for compatibility.

Unlike the standard Uno application templates, this template leverages Microsft.Extensions in order to provide Hosting, Dependency Injection, Configuration, Logging and other services.

In addition the template also provides guidance on how to separate the concerns of the view (i.e. pages and controls) from the corresponding viewmodel. The template leverages the work done by Microsoft and the community in the CommunityToolkit with specific reference to the Mvvm library that has been used to provide base implementations of INotifyPropertyChanged and INotifyDataErrorInfo interfaces.

The Uno.Extensions libraries follow the design of the Microsoft.Extensions libraries. Each library provides extensions for HostBuilder, ServiceCollection, ConfigurationBuilder, LoggingBuilder or other helper methods that can be used to add features to your application.

---

## Functional Overview

This is a summary of the functionalities available to the user.

_[Insert list of functionalities here]_

---

## Application Structure

_[Insert structure description here]_


---

## Topics

### T01 - Caching

_[Insert description of the caching here]_

### T02 - Offline

_[Insert description of the offline here]_

### T03 - Security

_[Insert description of the security here]_

### T04 - [Name of topic]

_[Insert description of topic here]_
