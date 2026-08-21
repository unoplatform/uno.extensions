using System.Dynamic;
using System.Text.Json;

namespace Playground.Models;

/// <summary>
/// Converts a raw JSON string (typically declared inline in XAML) into an <see cref="ExpandoObject"/>
/// graph exposed via <see cref="Value"/>, so it can be used as a mock DataContext / FeedView source.
/// </summary>
/// <remarks>
/// This deliberately deviates from the repo rule of typed JSON deserialization: the whole point of a
/// JSON mock is an unknown-at-compile-time shape, so no typed model can exist. Sample-only code —
/// see specs/009-feedview-mock-source/spec.md (Spec deviations).
/// </remarks>
public class JsonDataContext
{
	private string? _json;

	/// <summary>The raw JSON to convert.</summary>
	public string? Json
	{
		get => _json;
		set
		{
			_json = value;
			Value = Parse(value);
		}
	}

	/// <summary>The converted object graph (ExpandoObject for JSON objects, List for arrays, CLR primitives otherwise).</summary>
	public object? Value { get; private set; }

	private static object? Parse(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		try
		{
			using var document = JsonDocument.Parse(json);
			// The graph is fully materialized into CLR objects before the document is disposed.
			return ToClr(document.RootElement);
		}
		catch (JsonException error)
		{
			// Surface the parsing failure through the FeedView error state instead of crashing the page.
			return new Dictionary<string, object?> { ["Error"] = error };
		}
	}

	private static object? ToClr(JsonElement element)
		=> element.ValueKind switch
		{
			JsonValueKind.Object => ToExpando(element),
			JsonValueKind.Array => element.EnumerateArray().Select(ToClr).ToList(),
			JsonValueKind.String => element.GetString(),
			JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			_ => null,
		};

	private static ExpandoObject ToExpando(JsonElement element)
	{
		var expando = new ExpandoObject();
		var members = (IDictionary<string, object?>)expando;
		foreach (var property in element.EnumerateObject())
		{
			members[property.Name] = ToClr(property.Value);
		}

		return expando;
	}
}
