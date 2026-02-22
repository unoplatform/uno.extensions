#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

/// <summary>
/// Provides C#-specific naming conventions, type mappings, reserved-word
/// escaping, and code-generation helpers used throughout the refinement and
/// emission phases.
/// <para>
/// This service centralises the knowledge of how the language-agnostic
/// CodeDOM types map to idiomatic C# constructs. It is consumed by:
/// <list type="bullet">
///   <item><see cref="CSharpRefiner"/> — applies naming conventions and type
///   transformations to the CodeDOM tree.</item>
///   <item>All sub-emitters under <c>Emitter/</c> — format type references,
///   nullable guards, access modifiers, and <c>global::</c> prefixed names.</item>
/// </list>
/// </para>
/// </summary>
internal static class CSharpConventionService
{
	// ==================================================================
	// Version stamp emitted in [GeneratedCode("Kiota", "...")] attributes
	// ==================================================================

	/// <summary>
	/// The version string emitted in <c>[GeneratedCode("Kiota", ...)]</c>
	/// attributes on all generated types. Kept aligned with the Kiota runtime
	/// packages version used by the project.
	/// </summary>
	internal const string GeneratorVersion = "1.0.0";

	/// <summary>
	/// The tool name emitted in <c>[GeneratedCode]</c> attributes.
	/// </summary>
	internal const string GeneratorName = "Kiota";

	// ==================================================================
	// C# reserved words & contextual keywords
	// ==================================================================

	/// <summary>
	/// The complete set of C# reserved keywords that cannot be used as
	/// identifiers without an <c>@</c> prefix.
	/// </summary>
	private static readonly HashSet<string> ReservedWords = new HashSet<string>(StringComparer.Ordinal)
	{
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch",
		"char", "checked", "class", "const", "continue", "decimal", "default",
		"delegate", "do", "double", "else", "enum", "event", "explicit",
		"extern", "false", "finally", "fixed", "float", "for", "foreach",
		"goto", "if", "implicit", "in", "int", "interface", "internal", "is",
		"lock", "long", "namespace", "new", "null", "object", "operator",
		"out", "override", "params", "private", "protected", "public",
		"readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
		"stackalloc", "static", "string", "struct", "switch", "this",
		"throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
		"unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
	};

	/// <summary>
	/// Contextual keywords that are reserved in specific contexts but can
	/// still cause confusion. These too will get the <c>@</c> prefix when
	/// used as identifiers.
	/// </summary>
	private static readonly HashSet<string> ContextualKeywords = new HashSet<string>(StringComparer.Ordinal)
	{
		"add", "alias", "ascending", "async", "await", "by", "descending",
		"dynamic", "equals", "from", "get", "global", "group", "into",
		"join", "let", "nameof", "on", "orderby", "partial", "remove",
		"select", "set", "unmanaged", "value", "var", "when", "where",
		"yield",
	};

	// ==================================================================
	// OpenAPI → C# type mapping
	// ==================================================================

	/// <summary>
	/// Mapping from OpenAPI (type, format) pairs to C# type names used in
	/// the CodeDOM. The key is <c>"type:format"</c> with an empty format
	/// represented as <c>"type:"</c>.
	/// </summary>
	private static readonly Dictionary<string, TypeMapping> TypeMappings =
		new Dictionary<string, TypeMapping>(StringComparer.OrdinalIgnoreCase)
		{
			// string types
			["string:"] = new TypeMapping("string", isValueType: false, isNullable: true),
			["string:date-time"] = new TypeMapping("DateTimeOffset", isValueType: true, isNullable: true),
			["string:date"] = new TypeMapping("Date", isValueType: true, isNullable: true),
			["string:time"] = new TypeMapping("Time", isValueType: true, isNullable: true),
			["string:duration"] = new TypeMapping("TimeSpan", isValueType: true, isNullable: true),
			["string:uuid"] = new TypeMapping("Guid", isValueType: true, isNullable: true),
			["string:binary"] = new TypeMapping("Stream", isValueType: false, isNullable: true),
			["string:byte"] = new TypeMapping("byte", isValueType: true, isNullable: true, isCollection: true, collectionKind: CollectionKind.Array),
			["string:base64url"] = new TypeMapping("byte", isValueType: true, isNullable: true, isCollection: true, collectionKind: CollectionKind.Array),

			// integer types
			["integer:"] = new TypeMapping("int", isValueType: true, isNullable: true),
			["integer:int32"] = new TypeMapping("int", isValueType: true, isNullable: true),
			["integer:int64"] = new TypeMapping("long", isValueType: true, isNullable: true),

			// number types
			["number:"] = new TypeMapping("double", isValueType: true, isNullable: true),
			["number:float"] = new TypeMapping("float", isValueType: true, isNullable: true),
			["number:double"] = new TypeMapping("double", isValueType: true, isNullable: true),
			["number:decimal"] = new TypeMapping("decimal", isValueType: true, isNullable: true),

			// boolean
			["boolean:"] = new TypeMapping("bool", isValueType: true, isNullable: true),
		};

	/// <summary>
	/// Maps a <see cref="CodeTypeBase.Name"/> (CodeDOM type name) to the
	/// correct <c>IParseNode.GetXxxValue()</c> method name used in
	/// <c>GetFieldDeserializers()</c>.
	/// </summary>
	private static readonly Dictionary<string, string> ParseNodeMethodNames =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["string"] = "GetStringValue",
			["bool"] = "GetBoolValue",
			["int"] = "GetIntValue",
			["long"] = "GetLongValue",
			["float"] = "GetFloatValue",
			["double"] = "GetDoubleValue",
			["decimal"] = "GetDecimalValue",
			["Guid"] = "GetGuidValue",
			["DateTimeOffset"] = "GetDateTimeOffsetValue",
			["Date"] = "GetDateValue",
			["Time"] = "GetTimeValue",
			["TimeSpan"] = "GetTimeSpanValue",
			["byte"] = "GetByteArrayValue",
			["sbyte"] = "GetSbyteValue",
			["Stream"] = "GetByteArrayValue",
		};

	/// <summary>
	/// Maps a <see cref="CodeTypeBase.Name"/> (CodeDOM type name) to the
	/// correct <c>ISerializationWriter.WriteXxxValue()</c> method name used
	/// in <c>Serialize()</c>.
	/// </summary>
	private static readonly Dictionary<string, string> SerializationWriterMethodNames =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["string"] = "WriteStringValue",
			["bool"] = "WriteBoolValue",
			["int"] = "WriteIntValue",
			["long"] = "WriteLongValue",
			["float"] = "WriteFloatValue",
			["double"] = "WriteDoubleValue",
			["decimal"] = "WriteDecimalValue",
			["Guid"] = "WriteGuidValue",
			["DateTimeOffset"] = "WriteDateTimeOffsetValue",
			["Date"] = "WriteDateValue",
			["Time"] = "WriteTimeValue",
			["TimeSpan"] = "WriteTimeSpanValue",
			["byte"] = "WriteByteArrayValue",
			["sbyte"] = "WriteSbyteValue",
			["Stream"] = "WriteByteArrayValue",
		};

	/// <summary>
	/// The set of type names that are considered "primitive" in the Kiota
	/// serialization model — they use <c>GetXxxValue()</c> /
	/// <c>WriteXxxValue()</c> directly rather than the object-value
	/// methods.
	/// </summary>
	private static readonly HashSet<string> PrimitiveTypes =
		new HashSet<string>(StringComparer.Ordinal)
		{
			"string", "bool", "int", "long", "float", "double", "decimal",
			"Guid", "DateTimeOffset", "Date", "Time", "TimeSpan",
			"byte", "sbyte", "Stream",
		};

	/// <summary>
	/// The set of type names that are C# value types (structs) and should be
	/// emitted as nullable (<c>int?</c>) when the schema marks them nullable.
	/// </summary>
	private static readonly HashSet<string> ValueTypes =
		new HashSet<string>(StringComparer.Ordinal)
		{
			"bool", "int", "long", "float", "double", "decimal",
			"Guid", "DateTimeOffset", "Date", "Time", "TimeSpan",
			"byte", "sbyte", "short", "ushort", "uint", "ulong",
		};

	// ==================================================================
	// Public API — Type mapping
	// ==================================================================

	/// <summary>
	/// Resolves an OpenAPI type/format pair to the corresponding C# type
	/// information.
	/// </summary>
	/// <param name="openApiType">
	/// The OpenAPI <c>type</c> value (e.g., <c>"string"</c>, <c>"integer"</c>).
	/// </param>
	/// <param name="openApiFormat">
	/// The OpenAPI <c>format</c> value (e.g., <c>"date-time"</c>), or
	/// <see langword="null"/> if no format is specified.
	/// </param>
	/// <returns>
	/// A <see cref="TypeMapping"/> with the C# type name, value-type flag,
	/// nullability, and collection information; or <see langword="null"/> if
	/// the combination is not a known primitive mapping (indicating the caller
	/// should create a <see cref="CodeClass"/> for an <c>object</c> schema).
	/// </returns>
	public static TypeMapping GetTypeMapping(string openApiType, string openApiFormat)
	{
		if (string.IsNullOrEmpty(openApiType))
		{
			return null;
		}

		var key = openApiType + ":" + (openApiFormat ?? string.Empty);
		if (TypeMappings.TryGetValue(key, out var mapping))
		{
			return mapping;
		}

		// Fall back to type-only lookup (no format).
		var fallbackKey = openApiType + ":";
		if (TypeMappings.TryGetValue(fallbackKey, out var fallbackMapping))
		{
			return fallbackMapping;
		}

		return null;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the given CodeDOM type name is a
	/// known C# primitive in the Kiota serialization model.
	/// </summary>
	/// <param name="typeName">The CodeDOM type name (e.g., <c>"string"</c>).</param>
	/// <returns>
	/// <see langword="true"/> when the type is handled directly by
	/// <c>GetXxxValue()</c> / <c>WriteXxxValue()</c> methods; otherwise
	/// <see langword="false"/>.
	/// </returns>
	public static bool IsPrimitiveType(string typeName)
		=> !string.IsNullOrEmpty(typeName) && PrimitiveTypes.Contains(typeName);

	/// <summary>
	/// Returns <see langword="true"/> when the given CodeDOM type name is a
	/// C# value type (struct).
	/// </summary>
	/// <param name="typeName">The CodeDOM type name.</param>
	public static bool IsValueType(string typeName)
		=> !string.IsNullOrEmpty(typeName) && ValueTypes.Contains(typeName);

	// ==================================================================
	// Public API — Serialization / deserialization method dispatch
	// ==================================================================

	/// <summary>
	/// Returns the <c>IParseNode.GetXxxValue()</c> method name to use for
	/// deserializing a property of the given type.
	/// </summary>
	/// <param name="type">The property type reference.</param>
	/// <returns>
	/// The method name (e.g., <c>"GetStringValue"</c>), or
	/// <see langword="null"/> when the type requires
	/// <c>GetObjectValue</c> / <c>GetEnumValue</c> (callers handle those
	/// cases based on whether the type resolves to a CodeClass or CodeEnum).
	/// </returns>
	public static string GetDeserializationMethodName(CodeTypeBase type)
	{
		if (type is null)
		{
			return null;
		}

		if (type is CodeType codeType && ParseNodeMethodNames.TryGetValue(codeType.Name, out var method))
		{
			return method;
		}

		return null;
	}

	/// <summary>
	/// Returns the <c>ISerializationWriter.WriteXxxValue()</c> method name
	/// to use for serializing a property of the given type.
	/// </summary>
	/// <param name="type">The property type reference.</param>
	/// <returns>
	/// The method name (e.g., <c>"WriteStringValue"</c>), or
	/// <see langword="null"/> when the type requires
	/// <c>WriteObjectValue</c> / <c>WriteEnumValue</c>.
	/// </returns>
	public static string GetSerializationMethodName(CodeTypeBase type)
	{
		if (type is null)
		{
			return null;
		}

		if (type is CodeType codeType && SerializationWriterMethodNames.TryGetValue(codeType.Name, out var method))
		{
			return method;
		}

		return null;
	}

	// ==================================================================
	// Public API — Naming conventions
	// ==================================================================

	/// <summary>
	/// Converts a string to PascalCase. Handles snake_case, camelCase,
	/// kebab-case, and preserves existing PascalCase.
	/// </summary>
	/// <param name="input">The input string to convert.</param>
	/// <returns>A PascalCase version of <paramref name="input"/>.</returns>
	public static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input;
		}

		var sb = new StringBuilder(input.Length);
		bool capitalizeNext = true;

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			if (c == '_' || c == '-' || c == ' ' || c == '.')
			{
				// Separator character — capitalize the next actual character.
				capitalizeNext = true;
				continue;
			}

			if (capitalizeNext)
			{
				sb.Append(char.ToUpperInvariant(c));
				capitalizeNext = false;
			}
			else
			{
				// Detect camelCase boundaries: a lowercase followed by uppercase
				// We just pass the character as-is to preserve existing casing.
				sb.Append(c);
			}
		}

		return sb.Length > 0 ? sb.ToString() : input;
	}

	/// <summary>
	/// Converts a string to camelCase (first letter lowercase, rest follows
	/// PascalCase rules).
	/// </summary>
	/// <param name="input">The input string to convert.</param>
	/// <returns>A camelCase version of <paramref name="input"/>.</returns>
	public static string ToCamelCase(string input)
	{
		var pascal = ToPascalCase(input);
		if (string.IsNullOrEmpty(pascal))
		{
			return pascal;
		}

		return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
	}

	/// <summary>
	/// Converts a string from an OpenAPI identifier to a valid C# identifier,
	/// replacing invalid characters with underscores.
	/// </summary>
	/// <param name="input">The raw identifier.</param>
	/// <returns>A string safe to use as a C# identifier.</returns>
	public static string ToValidIdentifier(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return "_";
		}

		var sb = new StringBuilder(input.Length);

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			if (i == 0)
			{
				// First character must be a letter or underscore.
				if (char.IsLetter(c) || c == '_')
				{
					sb.Append(c);
				}
				else if (char.IsDigit(c))
				{
					sb.Append('_');
					sb.Append(c);
				}
				else
				{
					sb.Append('_');
				}
			}
			else
			{
				if (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c);
				}
				else
				{
					sb.Append('_');
				}
			}
		}

		return sb.Length > 0 ? sb.ToString() : "_";
	}

	// ==================================================================
	// Public API — Reserved word escaping
	// ==================================================================

	/// <summary>
	/// Returns <see langword="true"/> when <paramref name="name"/> is a C#
	/// reserved keyword or contextual keyword that must be escaped with
	/// <c>@</c> when used as an identifier.
	/// </summary>
	/// <param name="name">The identifier to check.</param>
	public static bool IsReservedWord(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}

		return ReservedWords.Contains(name) || ContextualKeywords.Contains(name);
	}

	/// <summary>
	/// Escapes <paramref name="name"/> with an <c>@</c> prefix if it is a C#
	/// reserved keyword or contextual keyword.
	/// </summary>
	/// <param name="name">The identifier to escape if necessary.</param>
	/// <returns>
	/// The escaped identifier (e.g., <c>"@class"</c>) or the original name
	/// if no escaping is needed.
	/// </returns>
	public static string EscapeReservedWord(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}

		if (IsReservedWord(name))
		{
			return "@" + name;
		}

		return name;
	}

	// ==================================================================
	// Public API — global:: prefix and fully qualified type names
	// ==================================================================

	/// <summary>
	/// Returns the <c>global::</c>-prefixed fully qualified type name for a
	/// CodeDOM element (class or enum) based on its namespace ancestry.
	/// <para>
	/// Example: <c>"global::MyApp.PetStore.Models.Pet"</c>
	/// </para>
	/// </summary>
	/// <param name="element">
	/// The CodeDOM element (typically a <see cref="CodeClass"/> or
	/// <see cref="CodeEnum"/>). Must not be <see langword="null"/>.
	/// </param>
	/// <returns>
	/// The fully qualified C# type name with <c>global::</c> prefix.
	/// </returns>
	public static string GetGloballyQualifiedName(CodeElement element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		return "global::" + GetFullyQualifiedName(element);
	}

	/// <summary>
	/// Returns the fully qualified name for a CodeDOM element by walking up
	/// the parent chain from the element through inner class nesting and
	/// namespace containers, joining each segment with a dot.
	/// <para>
	/// Example: <c>"MyApp.PetStore.Models.Pet"</c>
	/// </para>
	/// </summary>
	/// <param name="element">
	/// The CodeDOM element. Must not be <see langword="null"/>.
	/// </param>
	/// <returns>
	/// The dot-separated fully qualified name without <c>global::</c>.
	/// </returns>
	public static string GetFullyQualifiedName(CodeElement element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		var segments = new List<string>();
		var current = element;

		while (current != null)
		{
			if (!string.IsNullOrEmpty(current.Name))
			{
				segments.Add(current.Name);
			}

			current = current.Parent;
		}

		segments.Reverse();
		return string.Join(".", segments);
	}

	/// <summary>
	/// Returns the fully qualified type reference for a <see cref="CodeType"/>,
	/// resolving through its <see cref="CodeType.TypeDefinition"/> when
	/// available.
	/// <para>
	/// For external types (Kiota runtime, BCL) the type name is returned
	/// as-is. For internal types the full namespace-qualified name with
	/// <c>global::</c> prefix is returned.
	/// </para>
	/// </summary>
	/// <param name="type">The type reference.</param>
	/// <returns>
	/// A fully qualified C# type name suitable for emission.
	/// </returns>
	public static string GetTypeReference(CodeTypeBase type)
	{
		if (type is null)
		{
			return "object";
		}

		if (type is CodeType codeType)
		{
			return GetCodeTypeReference(codeType);
		}

		if (type is CodeUnionType)
		{
			// Union types are emitted as their wrapper class name.
			return type.Name;
		}

		if (type is CodeIntersectionType)
		{
			// Intersection types are emitted as their wrapper class name.
			return type.Name;
		}

		return type.Name;
	}

	/// <summary>
	/// Returns the full C# type declaration string including nullable
	/// markers and collection wrappers.
	/// <para>
	/// Examples: <c>"string?"</c>, <c>"int?"</c>,
	/// <c>"List&lt;global::MyApp.Models.Pet&gt;"</c>.
	/// </para>
	/// </summary>
	/// <param name="type">The type reference.</param>
	/// <param name="includeNullable">
	/// When <see langword="true"/> (default), value-type nullability is
	/// included (<c>int?</c>). When <see langword="false"/>, the raw type
	/// name is returned without <c>?</c>.
	/// </param>
	/// <returns>
	/// The C# type string suitable for use in property or parameter
	/// declarations.
	/// </returns>
	public static string GetTypeString(CodeTypeBase type, bool includeNullable = true)
	{
		if (type is null)
		{
			return "object";
		}

		var baseRef = GetTypeReference(type);

		// Collection wrapping.
		if (type.IsCollection)
		{
			if (type.CollectionKind == CollectionKind.Array)
			{
				baseRef = baseRef + "[]";
			}
			else
			{
				// Complex collection → List<T>
				baseRef = "List<" + baseRef + ">";
			}
		}

		// Nullable suffix for value types (including enums).
		if (includeNullable && type.IsNullable && !type.IsCollection)
		{
			if (IsValueType(GetBaseTypeName(type))
				|| (type is CodeType ct && ct.TypeDefinition is CodeEnum))
			{
				baseRef = baseRef + "?";
			}
		}

		return baseRef;
	}

	// ==================================================================
	// Public API — Access modifiers
	// ==================================================================

	/// <summary>
	/// Returns the C# keyword for the given <see cref="AccessModifier"/>.
	/// </summary>
	/// <param name="modifier">The access modifier.</param>
	/// <returns><c>"public"</c> or <c>"internal"</c>.</returns>
	public static string GetAccessModifier(AccessModifier modifier)
	{
		switch (modifier)
		{
			case AccessModifier.Internal:
				return "internal";
			default:
				return "public";
		}
	}

	// ==================================================================
	// Public API — Nullable conditional compilation guards
	// ==================================================================

	/// <summary>
	/// The preprocessor conditional for nullable reference type support.
	/// </summary>
	internal const string NullableEnableCondition =
		"NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER";

	/// <summary>
	/// Returns <see langword="true"/> when a property of the given type
	/// requires nullable conditional compilation guards
	/// (<c>#if NETSTANDARD2_1_OR_GREATER</c>) around its declaration.
	/// <para>
	/// This applies to nullable reference-type properties (e.g.,
	/// <c>string?</c>) but not to value-type properties (which use <c>?</c>
	/// unconditionally) or non-nullable properties.
	/// </para>
	/// </summary>
	/// <param name="type">The property type reference.</param>
	/// <returns>
	/// <see langword="true"/> when the property declaration should be
	/// wrapped in conditional compilation guards.
	/// </returns>
	public static bool RequiresNullableGuard(CodeTypeBase type)
	{
		if (type is null || !type.IsNullable)
		{
			return false;
		}

		var baseName = GetBaseTypeName(type);

		// Value types use ? directly — no guard needed.
		if (IsValueType(baseName))
		{
			return false;
		}

		// Enum types are value types in C# — no guard needed.
		if (type is CodeType codeType && codeType.TypeDefinition is CodeEnum)
		{
			return false;
		}

		// Collections that are reference types need guards.
		if (type.IsCollection)
		{
			return true;
		}

		// Reference types (string, object, models, etc.) need guards.
		return true;
	}

	// ==================================================================
	// Public API — Usings helpers
	// ==================================================================

	/// <summary>
	/// The standard set of <c>using</c> directives emitted for model classes.
	/// </summary>
	internal static readonly IReadOnlyList<string> ModelBaseUsings = new[]
	{
		"Microsoft.Kiota.Abstractions.Extensions",
		"Microsoft.Kiota.Abstractions.Serialization",
		"System.Collections.Generic",
		"System.IO",
		"System",
	};

	/// <summary>
	/// Minimal <c>using</c> directives for composed-type wrapper classes
	/// that contain only primitive properties (e.g., <c>SettingValue</c>
	/// with only <c>string</c>, <c>int</c>, <c>bool</c> members).
	/// </summary>
	internal static readonly IReadOnlyList<string> ComposedTypePrimitiveUsings = new[]
	{
		"Microsoft.Kiota.Abstractions.Serialization",
	};

	/// <summary>
	/// The set of <c>using</c> directives for model classes that reference
	/// types from <c>Microsoft.Kiota.Abstractions</c> (e.g., <c>Date</c>,
	/// <c>Time</c>, or <c>ApiException</c>).
	/// </summary>
	internal static readonly IReadOnlyList<string> ModelWithAbstractionsUsings = new[]
	{
		"Microsoft.Kiota.Abstractions.Extensions",
		"Microsoft.Kiota.Abstractions.Serialization",
		"Microsoft.Kiota.Abstractions",
		"System.Collections.Generic",
		"System.IO",
		"System",
	};

	/// <summary>
	/// The standard set of <c>using</c> directives emitted for request
	/// builder classes.
	/// </summary>
	internal static readonly IReadOnlyList<string> RequestBuilderUsings = new[]
	{
		"Microsoft.Kiota.Abstractions.Extensions",
		"Microsoft.Kiota.Abstractions.Serialization",
		"Microsoft.Kiota.Abstractions",
		"System.Collections.Generic",
		"System.IO",
		"System.Threading.Tasks",
		"System.Threading",
		"System",
	};

	/// <summary>
	/// The <c>using</c> directives for the root client class (includes
	/// serialization factory registrations).
	/// </summary>
	internal static readonly IReadOnlyList<string> ClientRootUsings = new[]
	{
		"Microsoft.Kiota.Abstractions.Extensions",
		"Microsoft.Kiota.Abstractions",
		"Microsoft.Kiota.Serialization.Form",
		"Microsoft.Kiota.Serialization.Json",
		"Microsoft.Kiota.Serialization.Multipart",
		"Microsoft.Kiota.Serialization.Text",
		"System.Collections.Generic",
		"System.IO",
		"System.Threading.Tasks",
		"System",
	};

	/// <summary>
	/// The <c>using</c> directives for enum types.
	/// </summary>
	internal static readonly IReadOnlyList<string> EnumUsings = new[]
	{
		"System.Runtime.Serialization",
		"System",
	};

	/// <summary>
	/// The set of type names from <c>Microsoft.Kiota.Abstractions</c> that
	/// are NOT in <c>Microsoft.Kiota.Abstractions.Serialization</c>. When a
	/// model uses one of these types, the model file needs
	/// <c>using Microsoft.Kiota.Abstractions;</c>.
	/// </summary>
	private static readonly HashSet<string> KiotaAbstractionsTypes =
		new HashSet<string>(StringComparer.Ordinal)
		{
			"Date", "Time", "ApiException",
		};

	/// <summary>
	/// Returns <see langword="true"/> when the model class references types
	/// that require <c>using Microsoft.Kiota.Abstractions;</c> (e.g.,
	/// <c>Date</c>, <c>Time</c>, or extends <c>ApiException</c>).
	/// </summary>
	/// <param name="cls">The class to check.</param>
	/// <returns><see langword="true"/> when the extra using is needed.</returns>
	internal static bool NeedsAbstractionsUsing(CodeClass cls)
	{
		if (cls is null)
		{
			return false;
		}

		// Check base class (e.g., ApiException for error models).
		if (cls.BaseClass != null && KiotaAbstractionsTypes.Contains(cls.BaseClass.Name))
		{
			return true;
		}

		// Check property types.
		for (int i = 0; i < cls.Properties.Count; i++)
		{
			var propType = cls.Properties[i].Type;
			if (propType != null && KiotaAbstractionsTypes.Contains(GetBaseTypeName(propType)))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Collects the fully-qualified namespaces of all internal types
	/// referenced by the class (via properties, methods, indexers, and
	/// navigation targets). Returns them sorted and deduplicated, excluding
	/// the class's own namespace.
	/// </summary>
	/// <param name="cls">The class to scan.</param>
	/// <param name="ownNamespace">
	/// The namespace of the class itself (to exclude from the result).
	/// </param>
	/// <returns>
	/// A sorted list of namespace strings to emit as local <c>using</c>
	/// directives.
	/// </returns>
	internal static IReadOnlyList<string> CollectLocalUsings(CodeClass cls, string ownNamespace)
	{
		var namespaces = new HashSet<string>(StringComparer.Ordinal);

		CollectNamespacesFromType(cls.BaseClass, ownNamespace, namespaces);

		for (int i = 0; i < cls.Properties.Count; i++)
		{
			CollectNamespacesFromType(cls.Properties[i].Type, ownNamespace, namespaces);
		}

		for (int i = 0; i < cls.Methods.Count; i++)
		{
			var method = cls.Methods[i];
			CollectNamespacesFromType(method.ReturnType, ownNamespace, namespaces);
			for (int j = 0; j < method.Parameters.Count; j++)
			{
				CollectNamespacesFromType(method.Parameters[j].Type, ownNamespace, namespaces);
			}
		}

		for (int i = 0; i < cls.Indexers.Count; i++)
		{
			CollectNamespacesFromType(cls.Indexers[i].ReturnType, ownNamespace, namespaces);
		}

		var sorted = new List<string>(namespaces);
		sorted.Sort(StringComparer.Ordinal);
		return sorted;
	}

	/// <summary>
	/// If the <paramref name="type"/> is an internal CodeType with a
	/// resolved <see cref="CodeType.TypeDefinition"/>, extracts its parent
	/// namespace and adds it to <paramref name="result"/> (unless it matches
	/// <paramref name="ownNamespace"/>).
	/// </summary>
	private static void CollectNamespacesFromType(
		CodeTypeBase type,
		string ownNamespace,
		HashSet<string> result)
	{
		if (type is CodeType codeType && !codeType.IsExternal && codeType.TypeDefinition != null)
		{
			var fqn = GetFullyQualifiedName(codeType.TypeDefinition);
			var lastDot = fqn.LastIndexOf('.');
			if (lastDot > 0)
			{
				var ns = fqn.Substring(0, lastDot);
				if (!string.Equals(ns, ownNamespace, StringComparison.Ordinal))
				{
					result.Add(ns);
				}
			}
		}
	}

	// ==================================================================
	// Public API — File / hint name helpers
	// ==================================================================

	/// <summary>
	/// Computes the source generator hint name for a given CodeDOM element.
	/// <para>
	/// The hint name is used by Roslyn to uniquely identify generated source
	/// files. Format: <c>{Namespace}.{TypeName}.g.cs</c>
	/// </para>
	/// </summary>
	/// <param name="element">
	/// The CodeElement (class or enum) to generate a hint name for.
	/// </param>
	/// <returns>The hint name string.</returns>
	public static string GetHintName(CodeElement element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		return GetFullyQualifiedName(element) + ".g.cs";
	}

	/// <summary>
	/// Computes the file name for writing the generated code to disk
	/// (Phase 1 / CLI output). Returns just the type name with <c>.cs</c>.
	/// </summary>
	/// <param name="element">
	/// The CodeElement (class or enum) to generate a file name for.
	/// </param>
	/// <returns>The file name string (e.g., <c>"Pet.cs"</c>).</returns>
	public static string GetFileName(CodeElement element)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		return element.Name + ".cs";
	}

	// ==================================================================
	// Public API — Enum naming
	// ==================================================================

	/// <summary>
	/// Converts a raw OpenAPI enum value (e.g., <c>"some-dashed-value"</c>)
	/// to a valid C# enum member name (e.g., <c>"Some_dashed_value"</c>).
	/// <para>
	/// The conversion replaces characters invalid in C# identifiers with
	/// underscores and ensures the first character is a letter or underscore.
	/// This matches Kiota's convention of preserving the general shape while
	/// replacing invalid chars with underscores.
	/// </para>
	/// </summary>
	/// <param name="value">The raw enum string value.</param>
	/// <returns>A valid C# enum member name.</returns>
	public static string ToEnumMemberName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "_";
		}

		var sb = new StringBuilder(value.Length);

		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];

			if (i == 0)
			{
				if (char.IsLetter(c))
				{
					// Capitalize first letter to follow PascalCase.
					sb.Append(char.ToUpperInvariant(c));
				}
				else if (c == '_')
				{
					sb.Append(c);
				}
				else if (char.IsDigit(c))
				{
					sb.Append('_');
					sb.Append(c);
				}
				else
				{
					sb.Append('_');
				}
			}
			else
			{
				if (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c);
				}
				else
				{
					sb.Append('_');
				}
			}
		}

		var result = sb.Length > 0 ? sb.ToString() : "_";

		// Escape if the member name is a reserved word.
		return EscapeReservedWord(result);
	}

	// ==================================================================
	// Public API — Method signature helpers
	// ==================================================================

	/// <summary>
	/// Returns the HTTP method enum reference string for use in
	/// <c>new RequestInformation(Method.{METHOD}, ...)</c>.
	/// </summary>
	/// <param name="httpMethod">
	/// The HTTP method string (e.g., <c>"GET"</c>, <c>"POST"</c>).
	/// </param>
	/// <returns>
	/// The <c>Method.{METHOD}</c> constant reference (e.g.,
	/// <c>"Method.GET"</c>).
	/// </returns>
	public static string GetHttpMethodConstant(string httpMethod)
	{
		if (string.IsNullOrEmpty(httpMethod))
		{
			return "Method.GET";
		}

		return "Method." + httpMethod.ToUpperInvariant();
	}

	/// <summary>
	/// Returns the conventional async method name for an HTTP executor
	/// method (e.g., <c>"GET"</c> → <c>"GetAsync"</c>).
	/// </summary>
	/// <param name="httpMethod">
	/// The HTTP method string (e.g., <c>"GET"</c>, <c>"POST"</c>).
	/// </param>
	/// <returns>
	/// The PascalCase async method name (e.g., <c>"GetAsync"</c>,
	/// <c>"PostAsync"</c>).
	/// </returns>
	public static string GetExecutorMethodName(string httpMethod)
	{
		if (string.IsNullOrEmpty(httpMethod))
		{
			return "GetAsync";
		}

		return ToPascalCase(httpMethod.ToLowerInvariant()) + "Async";
	}

	/// <summary>
	/// Returns the conventional request-information builder method name for
	/// an HTTP method (e.g., <c>"GET"</c> → <c>"ToGetRequestInformation"</c>).
	/// </summary>
	/// <param name="httpMethod">
	/// The HTTP method string (e.g., <c>"GET"</c>, <c>"POST"</c>).
	/// </param>
	/// <returns>
	/// The method name (e.g., <c>"ToGetRequestInformation"</c>).
	/// </returns>
	public static string GetRequestGeneratorMethodName(string httpMethod)
	{
		if (string.IsNullOrEmpty(httpMethod))
		{
			return "ToGetRequestInformation";
		}

		return "To" + ToPascalCase(httpMethod.ToLowerInvariant()) + "RequestInformation";
	}

	// ==================================================================
	// Public API — Property / parameter defaults
	// ==================================================================

	/// <summary>
	/// Returns the default value expression for a parameter based on its kind.
	/// </summary>
	/// <param name="kind">The parameter kind.</param>
	/// <returns>
	/// The default value expression (e.g., <c>"default"</c>) or
	/// <see langword="null"/> when no default applies.
	/// </returns>
	public static string GetParameterDefault(CodeParameterKind kind)
	{
		switch (kind)
		{
			case CodeParameterKind.Cancellation:
				return "default";
			case CodeParameterKind.RequestConfiguration:
				return "default";
			default:
				return null;
		}
	}

	// ==================================================================
	// Public API — XML doc helpers
	// ==================================================================

	/// <summary>
	/// Escapes a string for embedding in an XML documentation comment.
	/// Preserves known XML doc tags (<c>&lt;see cref&gt;</c>,
	/// <c>&lt;see langword&gt;</c>, <c>&lt;paramref&gt;</c>, etc.)
	/// while escaping all other angle brackets.
	/// </summary>
	/// <param name="text">The raw text to escape.</param>
	/// <returns>
	/// The XML-escaped text safe for use inside <c>&lt;summary&gt;</c>
	/// and <c>&lt;param&gt;</c> tags.
	/// </returns>
	public static string EscapeXmlDoc(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}

		// If the text contains known XML doc elements, don't escape —
		// these are pre-formatted XML doc strings built by the emitter.
		if (text.IndexOf("<see ", StringComparison.Ordinal) >= 0
			|| text.IndexOf("<paramref ", StringComparison.Ordinal) >= 0
			|| text.IndexOf("<c>", StringComparison.Ordinal) >= 0)
		{
			return text;
		}

		return text
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;");
	}

	// ==================================================================
	// Internal helpers
	// ==================================================================

	/// <summary>
	/// Gets the base type name from a <see cref="CodeTypeBase"/>, stripping
	/// collection and nullability metadata.
	/// </summary>
	private static string GetBaseTypeName(CodeTypeBase type)
	{
		if (type is CodeType codeType)
		{
			return codeType.Name;
		}

		return type.Name;
	}

	/// <summary>
	/// Resolves a <see cref="CodeType"/> to its full C# type reference.
	/// </summary>
	private static string GetCodeTypeReference(CodeType codeType)
	{
		// External types (BCL, Kiota runtime) — use the name as-is.
		if (codeType.IsExternal)
		{
			return codeType.Name;
		}

		// Internal types with a resolved TypeDefinition — use global:: prefix
		// with the fully qualified name.
		if (codeType.TypeDefinition != null)
		{
			return GetGloballyQualifiedName(codeType.TypeDefinition);
		}

		// Unresolved internal type — emit the name as-is (should not happen
		// after MapTypeDefinitions, but avoids crashes during debugging).
		return codeType.Name;
	}
}

// ==================================================================
// TypeMapping value object
// ==================================================================

/// <summary>
/// Describes the result of mapping an OpenAPI type/format pair to a C# type.
/// </summary>
internal sealed class TypeMapping
{
	/// <summary>
	/// Initializes a new <see cref="TypeMapping"/>.
	/// </summary>
	public TypeMapping(
		string csharpTypeName,
		bool isValueType,
		bool isNullable,
		bool isCollection = false,
		CollectionKind collectionKind = CollectionKind.None)
	{
		CSharpTypeName = csharpTypeName;
		IsValueType = isValueType;
		IsNullable = isNullable;
		IsCollection = isCollection;
		CollectionKind = collectionKind;
	}

	/// <summary>The C# type name (e.g., <c>"string"</c>, <c>"DateTimeOffset"</c>).</summary>
	public string CSharpTypeName { get; }

	/// <summary>Whether the C# type is a value type (struct).</summary>
	public bool IsValueType { get; }

	/// <summary>Whether the type should be emitted as nullable.</summary>
	public bool IsNullable { get; }

	/// <summary>Whether the type is a collection.</summary>
	public bool IsCollection { get; }

	/// <summary>The collection kind when <see cref="IsCollection"/> is true.</summary>
	public CollectionKind CollectionKind { get; }
}
