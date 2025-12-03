using System.Text.Json.Serialization;

namespace Uno.Extensions.Serialization;

/// <summary>
/// A source-generated JSON serializer context for common types used internally by the serialization infrastructure.
/// This enables AOT-compatible serialization for types like <see cref="string"/>, <see cref="string"/>[], and <see cref="bool"/>
/// without requiring reflection-based JSON serialization.
/// </summary>
[JsonSourceGenerationOptions]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(bool))]
internal sealed partial class CommonTypesJsonSerializerContext : JsonSerializerContext
{
}
