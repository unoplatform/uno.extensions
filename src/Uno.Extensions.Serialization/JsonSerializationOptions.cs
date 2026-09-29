using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Uno.Extensions.Serialization;

/// <summary>
/// Options to configure JSON serialization settings for <see cref="ServiceCollectionExtensions"/>
/// and <see cref="HostBuilderExtensions" />.
/// </summary>
public class JsonSerializationOptions
{
	// Template only — never handed to a serializer directly.
	//
	// `System.Text.Json` caches a `JsonTypeInfo` on the options instance for every type it (de)serializes
	// via reflection. If this shared static were handed to a serializer, it would accumulate `JsonTypeInfo`
	// for every app type across every host, pinning those types — and, for a downstream host that loads
	// previewed apps into their own collectible AssemblyLoadContexts, the app's ALC — for the process
	// lifetime. Callers must obtain a host-scoped copy via <see cref="CreateSerializerOptions"/>.
	internal static readonly JsonSerializerOptions DefaultSerializerOptions = new JsonSerializerOptions()
	{
		AllowTrailingCommas     = true,
		DefaultIgnoreCondition  = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
		NumberHandling          = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,

		// The JsonSerializerOptions.GetTypeInfo method is called directly and needs a defined resolver
		// setting the default resolver (reflection-based) but the user can overwrite it directly or by modifying
		// the TypeInfoResolverChain. Use JsonTypeInfoResolver.Combine() to produce an empty TypeInfoResolver.
		TypeInfoResolver        = JsonSerializer.IsReflectionEnabledByDefault
			? CreateDefaultTypeResolver()
			: JsonTypeInfoResolver.Combine(),
	};

	/// <summary>
	/// Creates a fresh, host-scoped copy of the default serializer options.
	/// </summary>
	/// <remarks>
	/// Each caller receives its own instance so the reflection-based `JsonTypeInfo` cache that
	/// <c>System.Text.Json</c> builds up while (de)serializing app types is confined to that host
	/// and released with it, rather than accumulating on the shared <see cref="DefaultSerializerOptions"/>
	/// template.
	/// </remarks>
	internal static JsonSerializerOptions CreateSerializerOptions()
		=> new JsonSerializerOptions(DefaultSerializerOptions);

	/// <summary>
	/// Gets the <see cref="JsonSerializerOptions"/>.
	/// </summary>
	public JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only used when JsonSerializer.IsReflectionEnabledByDefault=true.")]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Only used when JsonSerializer.IsReflectionEnabledByDefault=true.")]
	private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
		=> new DefaultJsonTypeInfoResolver();
}
