using System.Collections;
using Microsoft.Extensions.Configuration;

namespace Uno.Extensions;

/// <summary>
/// The service addresses Aspire passes to the app as <c>services__*</c> environment variables.
/// </summary>
internal static class ServiceAddresses
{
	private const string Prefix = "services__";

	/// <summary>
	/// The addresses as configuration keys, <c>services:api:http:0</c> for <c>services__api__http__0</c>.
	/// </summary>
	/// <remarks>
	/// Names are compared ignoring case: an Android app receives them in upper case.
	/// </remarks>
	internal static IEnumerable<KeyValuePair<string, string?>> FromEnvironment()
	{
		foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
		{
			if (variable.Key is string name && name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
			{
				yield return new(name.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal), variable.Value as string);
			}
		}
	}
}
