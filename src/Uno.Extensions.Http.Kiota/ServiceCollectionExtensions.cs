using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Uno.Extensions.Http.Kiota;

/// <summary>
/// Provides extension methods for registering Kiota clients within the <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

	/// <summary>
	/// Registers a Kiota client with the specified <paramref name="name"/> and endpoint options.
	/// </summary>
	/// <typeparam name="TClient">The Kiota client type to register.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to register the client with.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> providing the hosting context.</param>
	/// <param name="options">[Optional] The endpoint options for the client (loaded from appsettings if not specified).</param>
	/// <param name="name">[Optional] The name for locating endpoint information in appsettings.</param>
	/// <param name="configure">[Optional] A callback for configuring the endpoint.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> with the registered Kiota client.</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddKiotaClient<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient
	>(
		this IServiceCollection services,
		HostBuilderContext context,
		EndpointOptions? options = null,
		string? name = null,
		Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	)
		where TClient : class =>
		services.AddKiotaClientWithEndpoint<TClient, EndpointOptions>(context, options, name, configure);

	/// <summary>
	/// Registers a Kiota client with the specified <paramref name="name"/> and supports additional endpoint options.
	/// </summary>
	/// <typeparam name="TClient">The Kiota client type to register.</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register.</typeparam>
	/// <param name="services">The <see cref="IServiceCollection"/> to register the client with.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> providing the hosting context.</param>
	/// <param name="options">[Optional] The endpoint options for the client (loaded from appsettings if not specified).</param>
	/// <param name="name">[Optional] The name for locating endpoint information in appsettings.</param>
	/// <param name="configure">[Optional] A callback for configuring the endpoint.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> with the registered Kiota client.</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddKiotaClientWithEndpoint<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		TEndpoint
	>(
	this IServiceCollection services,
	HostBuilderContext context,
	TEndpoint? options = null,
	string? name = null,
	Func<IHttpClientBuilder, TEndpoint?, IHttpClientBuilder>? configure = null
)
	where TClient : class
	where TEndpoint : EndpointOptions, new()
	{
		services.AddKiotaHandlers();
		var clientName = name ?? typeof(TClient).FullName ?? "DefaultClient";

		return services.AddClientWithEndpoint<TClient, TEndpoint>(
			context,
			options,
			name: clientName,
			httpClientFactory: (s, c) => s
				.AddHttpClient<TClient>(clientName)
				.AddTypedClient((httpClient, sp) =>
				{
					var authProvider = new AnonymousAuthenticationProvider();

					var parseNodeFactory = new Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory();
					var serializationWriterFactory = new Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory();

					var requestAdapter = new HttpClientRequestAdapter(authProvider, parseNodeFactory, serializationWriterFactory, httpClient);

					return (TClient)Activator.CreateInstance(typeof(TClient), requestAdapter)!;

				})
				.AttachKiotaHandlers(),
			configure: configure
		);
	}
	/// <summary>
	/// Dynamically adds Kiota handlers to the service collection.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to register the handlers with.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> with the registered Kiota handlers.</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Cannot annotate IEnumerator<T>.Current.get")]
	private static IServiceCollection AddKiotaHandlers(this IServiceCollection services)
	{
		var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
		foreach (var handler in kiotaHandlers)
		{
			services.AddTransient(handler);
		}

		return services;
	}

	/// <summary>
	/// Attaches Kiota handlers to the <see cref="IHttpClientBuilder"/>.
	/// </summary>
	/// <param name="builder">The <see cref="IHttpClientBuilder"/> to attach the handlers to.</param>
	/// <returns>The updated <see cref="IHttpClientBuilder"/> with the attached Kiota handlers.</returns>
	private static IHttpClientBuilder AttachKiotaHandlers(this IHttpClientBuilder builder)
	{
		var kiotaHandlers = KiotaClientFactory.GetDefaultHandlerActivatableTypes();
		foreach (var handler in kiotaHandlers)
		{
			builder.AddHttpMessageHandler((sp) => (DelegatingHandler)sp.GetRequiredService(handler));
		}

		return builder;
	}

}
