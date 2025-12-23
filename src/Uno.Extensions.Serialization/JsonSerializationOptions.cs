using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Uno.Extensions.Serialization;

/// <summary>
/// Options to configure JSON serialization settings for <see cref="ServiceCollectionExtensions"/>
/// and <see cref="HostBuilderExtensions" />.
/// </summary>
public class JsonSerializationOptions
{
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
	/// Gets the <see cref="JsonSerializerOptions"/>.
	/// </summary>
	public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions(DefaultSerializerOptions);

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only used when JsonSerializer.IsReflectionEnabledByDefault=true.")]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "Only used when JsonSerializer.IsReflectionEnabledByDefault=true.")]
	private static IJsonTypeInfoResolver CreateDefaultTypeResolver()
		=> new DefaultJsonTypeInfoResolver();
}
