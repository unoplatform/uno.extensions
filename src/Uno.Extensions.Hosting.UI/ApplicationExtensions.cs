//
// Copyright (c) .NET Foundation and Contributors.
// Portions Copyright (c) Microsoft Corporation. All Rights Reserved.
// See LICENSE in the project root for license information.
//

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml;
#else
using Windows.UI.Xaml;
#endif

namespace Uno.Extensions.Hosting;

public static partial class ApplicationExtensions
{
	/// <summary>
	/// Creates an <see cref="IApplicationBuilder"/> for the application.
	/// </summary>
	/// <param name="app">The <see cref="Application"/></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs"/></param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args) =>
		new ApplicationBuilder(app, args.GetTyped(), Application.Current.GetType().Assembly);

	/// <summary>
	/// Creates an <see cref="IApplicationBuilder"/> for the application.
	/// </summary>
	/// <param name="app">The <see cref="Application"/></param>
	/// <param name="args">The <see cref="LaunchActivatedEventArgs"/></param>
	/// <param name="applicationAssembly">The application assembly</param>
	/// <returns></returns>
	public static IApplicationBuilder CreateBuilder(this Application app, LaunchActivatedEventArgs args, Assembly applicationAssembly) =>
		new ApplicationBuilder(app, args.GetTyped(), applicationAssembly);

	/// <summary>
	/// Configures the application to use a custom Window type.
	/// </summary>
	/// <typeparam name="T">The type of the custom Window to use.</typeparam>
	/// <param name="builder">The application builder.</param>
	/// <returns>The same application builder for chaining.</returns>
	public static IApplicationBuilder UseWindow<T>(this IApplicationBuilder builder) where T : Window
	{
		builder.Services.AddSingleton<Window, T>();
		return builder;
	}
}
