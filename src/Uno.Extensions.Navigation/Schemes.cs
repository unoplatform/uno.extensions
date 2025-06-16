namespace Uno.Extensions.Navigation;
/// <summary>
/// Provides constants for various navigation qualifiers.
/// </summary>
public static class Qualifiers
{
	/// <summary>
	/// The separator used in navigation paths.
	/// <value>"/"</value>
	/// </summary>
	public const string Separator = "/";

	/// <summary>
	/// Represents the root of the navigation path.
	/// <value>"/"</value>
	/// </summary>
	public const string Root = "/";

	/// <summary>
	/// Represents no navigation qualifier.
	/// <value>""</value>
	/// </summary>
	public const string None = "";

	/// <summary>
	/// Represents a nested navigation path.
	/// <value>"./"</value>
	/// </summary>
	public const string Nested = "./";
	// Note: Disabling parent routing - leaving this code in case parent routing is required
	//public const string Parent = "../";

	/// <summary>
	/// Navigates the specified route and presenting it in form of a Dialog.
	/// <value>"!"</value>
	/// </summary>
	public const string Dialog = "!";

	/// <summary>
	/// Represents a navigation back action.
	/// <value>"-"</value>
	/// </summary>
	public const string NavigateBack = "-";

	/// <summary>
	/// Represents a clear back stack action.
	/// <value>"-/"</value>
	/// </summary>
	public const string ClearBackStack = "-/";
}
