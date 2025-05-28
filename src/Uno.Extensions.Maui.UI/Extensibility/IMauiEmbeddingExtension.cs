using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;

namespace Uno.Extensions.Maui.Extensibility;

/// <summary>  
/// Provides methods for embedding and initializing Maui applications within a platform-specific context.  
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMauiEmbeddingExtension
{
	/// <summary>  
	/// Initializes the Maui embedding extension with the specified application.  
	/// </summary>  
	/// <param name="iApp">The application to initialize.</param>  
	void Initialize(IApplication iApp) => throw new PlatformNotSupportedException();

	/// <summary>  
	/// Registers platform-specific services for the Maui application builder.  
	/// </summary>  
	/// <param name="builder">The Maui application builder.</param>  
	/// <param name="app">The application instance.</param>  
	/// <returns>The updated Maui application builder.</returns>  
	MauiAppBuilder RegisterPlatformServices(MauiAppBuilder builder, Application app) => throw new PlatformNotSupportedException();

	/// <summary>  
	/// Initializes the Maui embedding application with the specified parameters.  
	/// </summary>  
	/// <param name="mauiApp">The Maui application instance.</param>  
	/// <param name="app">The application instance.</param>  
	void InitializeMauiEmbeddingApp(MauiApp mauiApp, Application app) => throw new PlatformNotSupportedException();

	/// <summary>  
	/// Builds the Maui application using the specified builder, application, and window.  
	/// </summary>  
	/// <param name="builder">The Maui application builder.</param>  
	/// <param name="app">The application instance.</param>  
	/// <param name="window">The platform-specific window instance.</param>  
	/// <returns>The built Maui application.</returns>  
	MauiApp BuildMauiApp(MauiAppBuilder builder, Application app, Microsoft.UI.Xaml.Window window) => throw new PlatformNotSupportedException();

	/// <summary>  
	/// Handles changes to the source of a dependency object.  
	/// </summary>  
	/// <param name="dependencyObject">The dependency object whose source has changed.</param>  
	/// <param name="args">The event arguments containing details of the change.</param>  
	void OnSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) => throw new PlatformNotSupportedException();

	/// <summary>  
	/// Handles size changes for a visual element.  
	/// </summary>  
	/// <param name="element">The visual element whose size has changed.</param>  
	void OnSizeChanged(VisualElement? element) => throw new PlatformNotSupportedException();
}
