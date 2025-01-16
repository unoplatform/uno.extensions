namespace TestHarness.Ext.Navigation.AddressBar;

public class AddressBarUserService
{
	private readonly List<AddressBarUser> _users;

	public AddressBarUserService()
	{
		_users =
		[
			new(ConvertFromString("8a5c5b2e-ff96-474b-9e4d-65bde598f6bc"), "João Rodrigues"),
			new(ConvertFromString("2b64071a-2c8a-45e4-9f48-3eb7d7aace41"), "Ross Polard")
		];
	}

	public AddressBarUser? GetById(Guid id)
	{
		return _users.FirstOrDefault(user => user.UserId == id);
	}

	private static Guid ConvertFromString(string value)
	{
		var guid = Guid.Parse(value);
		return guid;
	}
}
