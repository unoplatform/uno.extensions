namespace Uno.Extensions.Navigation;

public static class Qualifiers
{
	public const string Separator = "/";
	public const string Root = "/";
	public const string None = "";
	public const string Nested = "./";
	// Note: Disabling parent routing - leaving this code in case parent routing is required
	//public const string Parent = "../";
	public const string Dialog = "!";
	public const string NavigateBack = "-";
	public const string ClearBackStack = "-/";
}
