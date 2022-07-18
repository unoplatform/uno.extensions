public record Auth
{
	public string? CallbackScheme { get; set; }
	public string? FacebookAppId { get; init; }
	public string? FacebookAppSecret { get; init; }
	public string? GoogleClientId { get; init; }
	public string? GoogleClientSecret { get; init; }
	public string? MicrosoftClientId { get; init; }
	public string? MicrosoftClientSecret { get; init; }
	public string? AppleClientId { get; init; }
	public string? AppleKeyId { get; init; }
	public string? AppleTeamId { get; init; }

}
