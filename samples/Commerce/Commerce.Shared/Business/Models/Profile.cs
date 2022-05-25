using Commerce.Data.Models;

namespace Commerce.Models;

public record Profile
{
	public Profile(ProfileData data)
	{
		FirstName = data.FirstName;
		LastName = data.LastName;
		Avatar = data.Avatar;
	}

	public string? FirstName { get; init; }
	public string? LastName { get; init; }
	public string? Avatar { get; init; }

	public string FullName => $"{FirstName} {LastName}";

}
