namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public partial record ChefsUser
{
	//	internal ChefsUser(UserData user)
	//	{
	//		Id = user.Id;
	//		UrlProfileImage = user.UrlProfileImage;
	//		FullName = user.FullName;
	//		Description = user.Description;
	//		Email = user.Email;
	//		PhoneNumber = user.PhoneNumber;
	//		Followers = user.Followers;
	//		Following = user.Following;
	//		Recipes = user.Recipes;
	//		IsCurrent = user.IsCurrent;
	//	}

	public Guid Id { get; init; }
	public string? UrlProfileImage { get; init; }
	public string? FullName { get; init; }
	public string? Description { get; init; }
	public string? Email { get; init; }
	public string? PhoneNumber { get; init; }
	public long? Followers { get; init; }
	public long? Following { get; init; }
	public long? Recipes { get; init; }
	public bool IsCurrent { get; init; }

	//internal UserData ToData() => new()
	//{
	//	Id = Id,
	//	UrlProfileImage = UrlProfileImage,
	//	FullName = FullName,
	//	Description = Description,
	//	Email = Email,
	//	PhoneNumber = PhoneNumber,
	//	Followers = Followers,
	//	Following = Following,
	//	Recipes = Recipes,
	//	IsCurrent = IsCurrent
	//};
}
