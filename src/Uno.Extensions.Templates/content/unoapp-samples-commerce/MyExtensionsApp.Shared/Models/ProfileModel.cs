//-:cnd:noEmit
namespace MyExtensionsApp.Models;

public record ProfileModel(Profile Profile)
{
	public string FullName => $"{Profile.FirstName} {Profile.LastName}";

	public string? Avatar => Profile.Avatar;
}
