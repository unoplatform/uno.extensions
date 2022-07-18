namespace Uno.Extensions.Authentication.WinUI.Web.Social;

public record FacebookOptions
{
	public const string DefaultName = "Facebook";
	public string? StartUri { get; set; }
	public string? CallbackUri { get; set; }
}
