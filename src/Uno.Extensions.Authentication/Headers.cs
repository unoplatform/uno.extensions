namespace Uno.Extensions.Authentication;

/// <summary>
/// Contains values which correspond to headers commonly used by the authentication feature.
/// </summary>
public static class Headers
{
	internal const string NoRefreshKey = "No-Refresh";

	/// <summary>
	/// Defines a header value which indicates that the tokens should not be refreshed.
	/// </summary>
	public const string NoRefresh = $"{NoRefreshKey}:true";
}
