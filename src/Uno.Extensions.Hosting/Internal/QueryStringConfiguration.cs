namespace Uno.Extensions.Hosting.Internal;

/// <summary>
/// Turns the query string of the app's launch URL into host configuration values.
/// </summary>
internal static class QueryStringConfiguration
{
	/// <summary>
	/// Reads every parameter of <paramref name="query"/> as a configuration key/value pair.
	/// </summary>
	/// <remarks>
	/// A parameter written without a value (<c>?flag</c>) is read as an empty value, the same as <c>?flag=</c>.
	/// Empty segments (a trailing or doubled <c>&amp;</c>) are skipped.
	/// </remarks>
	/// <param name="query">The query part of a URL, with or without its leading <c>?</c>.</param>
	/// <returns>The parameters, keyed by name.</returns>
	public static IDictionary<string, string?> Parse(string query)
	{
		var parameters = System.Web.HttpUtility.ParseQueryString(query);
		var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

		foreach (var key in parameters.AllKeys)
		{
			if (key is not null)
			{
				values[key] = parameters[key];
			}
		}

		// ParseQueryString files every segment that has no '=' under a null key, with the segment as its value.
		foreach (var bareKey in parameters.GetValues(null) ?? [])
		{
			if (bareKey.Length > 0)
			{
				values.TryAdd(bareKey, string.Empty);
			}
		}

		return values;
	}
}
